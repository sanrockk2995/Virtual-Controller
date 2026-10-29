using System;
using System.Diagnostics;
using VirtualController.Profiles;

namespace VirtualController.Core
{
    /// <summary>
    /// Converts raw physical InputState into normalized ControllerState
    /// according to the active Profile rules (analog normalization, deadzones, curves, mappings).
    /// Supports assigning Keyboard or Mouse to Left Stick, Right Stick, or both.
    /// Uses dedicated MouseToStickTranslator instances for high-fidelity mouse-to-stick conversion.
    /// </summary>
    public class MappingEngine
    {
        private readonly MouseToStickTranslator _leftTranslator = new();
        private readonly MouseToStickTranslator _rightTranslator = new();
        private readonly MacroEngine _macroEngine = new();
        private long _lastTimestamp;

        public MacroEngine MacroEngine => _macroEngine;

        /// <summary>
        /// Transforms raw physical input into an Xbox 360 controller state.
        /// </summary>
        public ControllerState Process(InputState input, Profile profile, ControllerState? existingState = null, double? explicitDt = null)
        {
            var output = existingState ?? new ControllerState();
            output.Reset();

            if (input == null || profile == null)
            {
                Reset();
                return output;
            }

            long now = Stopwatch.GetTimestamp();
            double dt;
            if (explicitDt.HasValue && explicitDt.Value > 0)
            {
                dt = explicitDt.Value;
            }
            else if (_lastTimestamp == 0)
            {
                dt = 0.008;
            }
            else
            {
                dt = (double)(now - _lastTimestamp) / Stopwatch.Frequency;
                if (dt <= 0.0001 || dt > 0.1) dt = 0.008;
            }
            _lastTimestamp = now;

            var (mouseDx, mouseDy) = input.HarvestMouseDelta();

            // 1. Process Left Stick (Keyboard or Mouse)
            var (lx, ly) = ProcessStick(profile.LeftStick, input, mouseDx, mouseDy, _leftTranslator, dt);
            output.LeftThumbX = lx;
            output.LeftThumbY = ly;

            // 2. Process Right Stick (Keyboard or Mouse)
            var (rx, ry) = ProcessStick(profile.RightStick, input, mouseDx, mouseDy, _rightTranslator, dt);
            output.RightThumbX = rx;
            output.RightThumbY = ry;

            // 3. Process Buttons and Triggers
            ProcessButtons(input, profile, output);

            _macroEngine.Update(input, profile.Macros, output, dt);

            return output;
        }

        public static (double x, double y) CalculateKeyboardStick(
            bool up, bool down, bool left, bool right,
            double deadzone = 0.0, double sensitivity = 1.0, string curve = "Linear")
        {
            double rawX = 0.0;
            double rawY = 0.0;

            if (right) rawX += 1.0;
            if (left)  rawX -= 1.0;
            if (up)    rawY += 1.0;
            if (down)  rawY -= 1.0;

            if (rawX == 0.0 && rawY == 0.0)
            {
                return (0.0, 0.0);
            }

            double magnitude = Math.Sqrt(rawX * rawX + rawY * rawY);

            // Normalize diagonal magnitude so it never exceeds 1.0
            double dirX = rawX / magnitude;
            double dirY = rawY / magnitude;

            if (magnitude > 1.0)
            {
                magnitude = 1.0;
            }

            // Apply deadzone
            if (magnitude < deadzone)
            {
                return (0.0, 0.0);
            }

            // Scale magnitude above deadzone
            double scaledMag = (magnitude - deadzone) / (1.0 - deadzone);

            if (string.Equals(curve, "Exponential", StringComparison.OrdinalIgnoreCase))
            {
                scaledMag = scaledMag * scaledMag;
            }

            scaledMag *= sensitivity;
            if (scaledMag > 1.0) scaledMag = 1.0;

            double finalX = Math.Clamp(dirX * scaledMag, -1.0, 1.0);
            double finalY = Math.Clamp(dirY * scaledMag, -1.0, 1.0);

            return (finalX, finalY);
        }

        private (double x, double y) ProcessStick(
            StickConfig config,
            InputState input,
            int mouseDx,
            int mouseDy,
            MouseToStickTranslator translator,
            double dt)
        {
            if (config.IsDisabled)
            {
                translator.Reset();
                return (0.0, 0.0);
            }

            if (config.IsMouse)
            {
                return translator.Process(mouseDx, mouseDy, config, dt);
            }
            else
            {
                bool up = input.IsPhysicalInputActive(config.Up);
                bool down = input.IsPhysicalInputActive(config.Down);
                bool left = input.IsPhysicalInputActive(config.Left);
                bool right = input.IsPhysicalInputActive(config.Right);

                return CalculateKeyboardStick(
                    up, down, left, right,
                    config.Deadzone, config.Sensitivity, config.ResponseCurve);
            }
        }

        private void ProcessButtons(InputState input, Profile profile, ControllerState output)
        {
            // Primary button mapping: Virtual Button Name -> Physical Input Name
            foreach (var kvp in profile.Buttons)
            {
                string vButton = kvp.Key;
                string pInput = kvp.Value;

                if (input.IsPhysicalInputActive(pInput))
                {
                    ApplyVirtualButton(vButton, true, output);
                }
            }

            // Extra mappings: Physical Input Name -> Virtual Button Name
            foreach (var kvp in profile.ExtraMappings)
            {
                string pInput = kvp.Key;
                string vButton = kvp.Value;

                if (input.IsPhysicalInputActive(pInput))
                {
                    ApplyVirtualButton(vButton, true, output);
                }
            }
        }

        private void ApplyVirtualButton(string vButton, bool active, ControllerState output)
        {
            switch (vButton.ToUpperInvariant())
            {
                case "A": output.A = active; break;
                case "B": output.B = active; break;
                case "X": output.X = active; break;
                case "Y": output.Y = active; break;
                case "LB": output.LeftShoulder = active; break;
                case "RB": output.RightShoulder = active; break;
                case "LT": output.LeftTrigger = active ? 1.0 : 0.0; break;
                case "RT": output.RightTrigger = active ? 1.0 : 0.0; break;
                case "BACK": output.Back = active; break;
                case "START": output.Start = active; break;
                case "GUIDE": output.Guide = active; break;
                case "LS":
                case "LEFTTHUMB": output.LeftThumb = active; break;
                case "RS":
                case "RIGHTTHUMB": output.RightThumb = active; break;
                case "DPADUP": output.DPadUp = active; break;
                case "DPADDOWN": output.DPadDown = active; break;
                case "DPADLEFT": output.DPadLeft = active; break;
                case "DPADRIGHT": output.DPadRight = active; break;

                // Stick Directions
                case "LS_UP":
                case "LSUP":
                    if (active) output.LeftThumbY = 1.0;
                    break;
                case "LS_DOWN":
                case "LSDOWN":
                    if (active) output.LeftThumbY = -1.0;
                    break;
                case "LS_LEFT":
                case "LSLEFT":
                    if (active) output.LeftThumbX = -1.0;
                    break;
                case "LS_RIGHT":
                case "LSRIGHT":
                    if (active) output.LeftThumbX = 1.0;
                    break;

                case "RS_UP":
                case "RSUP":
                    if (active) output.RightThumbY = 1.0;
                    break;
                case "RS_DOWN":
                case "RSDOWN":
                    if (active) output.RightThumbY = -1.0;
                    break;
                case "RS_LEFT":
                case "RSLEFT":
                    if (active) output.RightThumbX = -1.0;
                    break;
                case "RS_RIGHT":
                case "RSRIGHT":
                    if (active) output.RightThumbX = 1.0;
                    break;
            }
        }

        public void Reset()
        {
            _lastTimestamp = 0;
            _leftTranslator.Reset();
            _rightTranslator.Reset();
            _macroEngine.Reset();
        }
    }
}
