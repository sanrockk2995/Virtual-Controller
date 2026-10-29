using System;
using System.Collections.Generic;
using VirtualController.Profiles;

namespace VirtualController.Core
{
    /// <summary>
    /// Deterministic, tick-based state machine engine that executes configured macros
    /// against raw inputs and applies virtual actions onto ControllerState.
    /// </summary>
    public class MacroEngine
    {
        /// <summary>
        /// Per-macro execution and timeline tracking state.
        /// </summary>
        public class MacroRuntimeState
        {
            public bool IsRunning { get; set; }
            public bool WasTriggerPressed { get; set; }
            public int CurrentStepIndex { get; set; }
            public double PhaseElapsedMs { get; set; }
            public bool InDelayPhase { get; set; }
            public bool InLoopDelayPhase { get; set; }
            public HashSet<string> HeldButtons { get; } = new(StringComparer.OrdinalIgnoreCase);

            public void Reset()
            {
                IsRunning = false;
                WasTriggerPressed = false;
                CurrentStepIndex = 0;
                PhaseElapsedMs = 0.0;
                InDelayPhase = false;
                InLoopDelayPhase = false;
                HeldButtons.Clear();
            }

            public void Start()
            {
                IsRunning = true;
                CurrentStepIndex = 0;
                PhaseElapsedMs = 0.0;
                InDelayPhase = false;
                InLoopDelayPhase = false;
                HeldButtons.Clear();
            }

            public void Stop()
            {
                IsRunning = false;
                CurrentStepIndex = 0;
                PhaseElapsedMs = 0.0;
                InDelayPhase = false;
                InLoopDelayPhase = false;
                HeldButtons.Clear();
            }
        }

        private readonly Dictionary<string, MacroRuntimeState> _states = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new();

        /// <summary>
        /// Evaluates all configured macros against current input, advances their execution timelines,
        /// and applies active macro outputs directly to output state.
        /// </summary>
        public void Update(InputState input, IReadOnlyList<MacroDefinition>? macros, ControllerState output, double dtSeconds)
        {
            if (input == null || output == null || macros == null || macros.Count == 0)
            {
                return;
            }

            double dtMs = (double.IsNaN(dtSeconds) || double.IsInfinity(dtSeconds) || dtSeconds <= 0.0)
                ? 0.0
                : dtSeconds * 1000.0;

            lock (_lock)
            {
                for (int i = 0; i < macros.Count; i++)
                {
                    var macro = macros[i];
                    if (macro == null) continue;

                    string macroId = !string.IsNullOrWhiteSpace(macro.Id) ? macro.Id : macro.Name;
                    if (string.IsNullOrWhiteSpace(macroId)) continue;

                    if (!_states.TryGetValue(macroId, out var state))
                    {
                        state = new MacroRuntimeState();
                        _states[macroId] = state;
                    }

                    // Disabled, unassigned, or empty steps -> stop and clean up immediately
                    if (!macro.IsEnabled ||
                        macro.Steps == null ||
                        macro.Steps.Count == 0 ||
                        string.IsNullOrWhiteSpace(macro.TriggerInput) ||
                        macro.TriggerInput.Equals("Unassigned", StringComparison.OrdinalIgnoreCase))
                    {
                        if (state.IsRunning || state.HeldButtons.Count > 0)
                        {
                            state.Stop();
                        }
                        state.WasTriggerPressed = false;
                        continue;
                    }

                    bool isPressed = input.IsPhysicalInputActive(macro.TriggerInput);
                    bool risingEdge = isPressed && !state.WasTriggerPressed;
                    bool fallingEdge = !isPressed && state.WasTriggerPressed;
                    state.WasTriggerPressed = isPressed;

                    // Mode trigger evaluation
                    switch (macro.Mode)
                    {
                        case MacroMode.PlayOnce:
                            if (risingEdge)
                            {
                                state.Start();
                            }
                            break;

                        case MacroMode.RepeatWhileHeld:
                            if (isPressed && !state.IsRunning)
                            {
                                state.Start();
                            }
                            else if (!isPressed && state.IsRunning)
                            {
                                state.Stop();
                            }
                            break;

                        case MacroMode.ToggleLoop:
                            if (risingEdge)
                            {
                                if (state.IsRunning)
                                {
                                    state.Stop();
                                }
                                else
                                {
                                    state.Start();
                                }
                            }
                            break;
                    }

                    if (state.IsRunning)
                    {
                        double remainingMs = dtMs;
                        int loopGuard = 0;

                        while (state.IsRunning)
                        {
                            if (++loopGuard > 1000)
                            {
                                break;
                            }

                            // 1. Inter-loop delay handling (between full step iterations)
                            if (state.InLoopDelayPhase)
                            {
                                double loopDelay = Math.Max(0, macro.LoopDelayMs);
                                if (state.PhaseElapsedMs + remainingMs < loopDelay)
                                {
                                    state.PhaseElapsedMs += remainingMs;
                                    remainingMs = 0.0;
                                    break;
                                }
                                else
                                {
                                    double timeUsed = Math.Max(0.0, loopDelay - state.PhaseElapsedMs);
                                    remainingMs -= timeUsed;
                                    state.InLoopDelayPhase = false;
                                    state.CurrentStepIndex = 0;
                                    state.PhaseElapsedMs = 0.0;
                                    state.InDelayPhase = false;
                                    if (remainingMs <= 0.0) break;
                                    continue;
                                }
                            }

                            // 2. Check if all steps completed
                            if (state.CurrentStepIndex >= macro.Steps.Count)
                            {
                                state.HeldButtons.Clear();

                                if (macro.Mode == MacroMode.PlayOnce)
                                {
                                    state.IsRunning = false;
                                    break;
                                }
                                else
                                {
                                    double loopDelay = Math.Max(0, macro.LoopDelayMs);
                                    if (loopDelay > 0)
                                    {
                                        state.InLoopDelayPhase = true;
                                        state.PhaseElapsedMs = 0.0;
                                        continue;
                                    }
                                    else
                                    {
                                        state.CurrentStepIndex = 0;
                                        state.PhaseElapsedMs = 0.0;
                                        state.InDelayPhase = false;
                                        if (remainingMs <= 0.0) break;
                                        continue;
                                    }
                                }
                            }

                            if (remainingMs <= 0.0)
                            {
                                break;
                            }

                            var step = macro.Steps[state.CurrentStepIndex];
                            if (step == null)
                            {
                                state.CurrentStepIndex++;
                                state.PhaseElapsedMs = 0.0;
                                state.InDelayPhase = false;
                                continue;
                            }

                            // 3. Process current step progression
                            switch (step.StepType)
                            {
                                case MacroStepType.Tap:
                                    if (!state.InDelayPhase)
                                    {
                                        // Holding phase
                                        double duration = Math.Max(1, step.DurationMs);
                                        if (state.PhaseElapsedMs + remainingMs < duration)
                                        {
                                            state.PhaseElapsedMs += remainingMs;
                                            remainingMs = 0.0;
                                        }
                                        else
                                        {
                                            double timeUsed = Math.Max(0.0, duration - state.PhaseElapsedMs);
                                            remainingMs -= timeUsed;
                                            state.InDelayPhase = true;
                                            state.PhaseElapsedMs = 0.0;
                                        }
                                    }
                                    else
                                    {
                                        // DelayAfter phase (tap buttons released)
                                        double delay = Math.Max(0, step.DelayAfterMs);
                                        if (state.PhaseElapsedMs + remainingMs < delay)
                                        {
                                            state.PhaseElapsedMs += remainingMs;
                                            remainingMs = 0.0;
                                        }
                                        else
                                        {
                                            double timeUsed = Math.Max(0.0, delay - state.PhaseElapsedMs);
                                            remainingMs -= timeUsed;
                                            state.InDelayPhase = false;
                                            state.PhaseElapsedMs = 0.0;
                                            state.CurrentStepIndex++;
                                        }
                                    }
                                    break;

                                case MacroStepType.PressDown:
                                    if (step.Buttons != null)
                                    {
                                        for (int b = 0; b < step.Buttons.Count; b++)
                                        {
                                            string btn = step.Buttons[b];
                                            if (!string.IsNullOrWhiteSpace(btn))
                                            {
                                                state.HeldButtons.Add(btn);
                                            }
                                        }
                                    }

                                    double pressDelay = Math.Max(0, step.DelayAfterMs);
                                    if (state.PhaseElapsedMs + remainingMs < pressDelay)
                                    {
                                        state.PhaseElapsedMs += remainingMs;
                                        remainingMs = 0.0;
                                    }
                                    else
                                    {
                                        double timeUsed = Math.Max(0.0, pressDelay - state.PhaseElapsedMs);
                                        remainingMs -= timeUsed;
                                        state.CurrentStepIndex++;
                                        state.PhaseElapsedMs = 0.0;
                                        state.InDelayPhase = false;
                                    }
                                    break;

                                case MacroStepType.ReleaseUp:
                                    if (step.Buttons != null)
                                    {
                                        for (int b = 0; b < step.Buttons.Count; b++)
                                        {
                                            string btn = step.Buttons[b];
                                            if (!string.IsNullOrWhiteSpace(btn))
                                            {
                                                state.HeldButtons.Remove(btn);
                                            }
                                        }
                                    }

                                    double releaseDelay = Math.Max(0, step.DelayAfterMs);
                                    if (state.PhaseElapsedMs + remainingMs < releaseDelay)
                                    {
                                        state.PhaseElapsedMs += remainingMs;
                                        remainingMs = 0.0;
                                    }
                                    else
                                    {
                                        double timeUsed = Math.Max(0.0, releaseDelay - state.PhaseElapsedMs);
                                        remainingMs -= timeUsed;
                                        state.CurrentStepIndex++;
                                        state.PhaseElapsedMs = 0.0;
                                        state.InDelayPhase = false;
                                    }
                                    break;

                                case MacroStepType.Wait:
                                    double waitDuration = Math.Max(1, step.DelayAfterMs > 0 ? step.DelayAfterMs : step.DurationMs);
                                    if (state.PhaseElapsedMs + remainingMs < waitDuration)
                                    {
                                        state.PhaseElapsedMs += remainingMs;
                                        remainingMs = 0.0;
                                    }
                                    else
                                    {
                                        double timeUsed = Math.Max(0.0, waitDuration - state.PhaseElapsedMs);
                                        remainingMs -= timeUsed;
                                        state.CurrentStepIndex++;
                                        state.PhaseElapsedMs = 0.0;
                                        state.InDelayPhase = false;
                                    }
                                    break;

                                default:
                                    state.CurrentStepIndex++;
                                    state.PhaseElapsedMs = 0.0;
                                    state.InDelayPhase = false;
                                    break;
                            }
                        }

                        // 4. Apply all active macro outputs directly onto ControllerState
                        if (state.IsRunning)
                        {
                            // Apply persistent held buttons
                            if (state.HeldButtons.Count > 0)
                            {
                                foreach (var btn in state.HeldButtons)
                                {
                                    ApplyVirtualAction(btn, output);
                                }
                            }

                            // Apply active tap buttons (only while holding phase is active)
                            if (!state.InLoopDelayPhase &&
                                !state.InDelayPhase &&
                                state.CurrentStepIndex >= 0 &&
                                state.CurrentStepIndex < macro.Steps.Count)
                            {
                                var curStep = macro.Steps[state.CurrentStepIndex];
                                if (curStep != null &&
                                    curStep.StepType == MacroStepType.Tap &&
                                    curStep.Buttons != null)
                                {
                                    for (int b = 0; b < curStep.Buttons.Count; b++)
                                    {
                                        ApplyVirtualAction(curStep.Buttons[b], output);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Maps standard virtual controller action or stick direction names to ControllerState.
        /// </summary>
        public static void ApplyVirtualAction(string vAction, ControllerState output)
        {
            if (string.IsNullOrWhiteSpace(vAction) || output == null) return;

            switch (vAction.Trim().ToUpperInvariant())
            {
                // Standard Digital Buttons
                case "A": output.A = true; break;
                case "B": output.B = true; break;
                case "X": output.X = true; break;
                case "Y": output.Y = true; break;

                case "LB":
                case "LEFTSHOULDER":
                    output.LeftShoulder = true; break;

                case "RB":
                case "RIGHTSHOULDER":
                    output.RightShoulder = true; break;

                case "LT":
                case "LEFTTRIGGER":
                    output.LeftTrigger = 1.0; break;

                case "RT":
                case "RIGHTTRIGGER":
                    output.RightTrigger = 1.0; break;

                case "BACK":
                case "VIEW":
                    output.Back = true; break;

                case "START":
                case "MENU":
                    output.Start = true; break;

                case "GUIDE":
                case "XBOX":
                    output.Guide = true; break;

                case "LS":
                case "LEFTTHUMB":
                case "L3":
                    output.LeftThumb = true; break;

                case "RS":
                case "RIGHTTHUMB":
                case "R3":
                    output.RightThumb = true; break;

                case "DPADUP":
                case "DPAD_UP":
                    output.DPadUp = true; break;

                case "DPADDOWN":
                case "DPAD_DOWN":
                    output.DPadDown = true; break;

                case "DPADLEFT":
                case "DPAD_LEFT":
                    output.DPadLeft = true; break;

                case "DPADRIGHT":
                case "DPAD_RIGHT":
                    output.DPadRight = true; break;

                // Stick Directions
                case "LS_UP":
                case "LSUP":
                    output.LeftThumbY = 1.0; break;

                case "LS_DOWN":
                case "LSDOWN":
                    output.LeftThumbY = -1.0; break;

                case "LS_LEFT":
                case "LSLEFT":
                    output.LeftThumbX = -1.0; break;

                case "LS_RIGHT":
                case "LSRIGHT":
                    output.LeftThumbX = 1.0; break;

                case "RS_UP":
                case "RSUP":
                    output.RightThumbY = 1.0; break;

                case "RS_DOWN":
                case "RSDOWN":
                    output.RightThumbY = -1.0; break;

                case "RS_LEFT":
                case "RSLEFT":
                    output.RightThumbX = -1.0; break;

                case "RS_RIGHT":
                case "RSRIGHT":
                    output.RightThumbX = 1.0; break;
            }
        }

        /// <summary>
        /// Returns true if the macro with the specified Id is actively executing.
        /// </summary>
        public bool IsMacroRunning(string macroId)
        {
            if (string.IsNullOrWhiteSpace(macroId)) return false;
            lock (_lock)
            {
                return _states.TryGetValue(macroId, out var state) && state.IsRunning;
            }
        }

        /// <summary>
        /// Stops the specified macro and clears its held buttons immediately.
        /// </summary>
        public void StopMacro(string macroId)
        {
            if (string.IsNullOrWhiteSpace(macroId)) return;
            lock (_lock)
            {
                if (_states.TryGetValue(macroId, out var state))
                {
                    state.Stop();
                }
            }
        }

        /// <summary>
        /// Clears all runtime macro states.
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                foreach (var state in _states.Values)
                {
                    state.Reset();
                }
                _states.Clear();
            }
        }

        /// <summary>
        /// Gets the current runtime state for a macro, or null if never executed.
        /// </summary>
        public MacroRuntimeState? GetState(string macroId)
        {
            if (string.IsNullOrWhiteSpace(macroId)) return null;
            lock (_lock)
            {
                return _states.TryGetValue(macroId, out var state) ? state : null;
            }
        }
    }
}
