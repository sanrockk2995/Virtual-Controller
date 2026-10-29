using System;
using System.Collections.Generic;

namespace VirtualController.Profiles
{
    public enum MacroMode
    {
        PlayOnce = 0,
        RepeatWhileHeld = 1,
        ToggleLoop = 2
    }

    public enum MacroStepType
    {
        Tap = 0,
        PressDown = 1,
        ReleaseUp = 2,
        Wait = 3
    }

    public class MacroStep
    {
        public MacroStepType StepType { get; set; } = MacroStepType.Tap;
        public List<string> Buttons { get; set; } = new();
        public int DurationMs { get; set; } = 50;
        public int DelayAfterMs { get; set; } = 50;

        public MacroStep Clone()
        {
            return new MacroStep
            {
                StepType = StepType,
                Buttons = Buttons != null ? new List<string>(Buttons) : new List<string>(),
                DurationMs = DurationMs,
                DelayAfterMs = DelayAfterMs
            };
        }

        public string GetDisplaySummary()
        {
            if (StepType == MacroStepType.Wait)
            {
                return $"WAIT ({DelayAfterMs} ms)";
            }

            string btns = (Buttons != null && Buttons.Count > 0)
                ? string.Join(" + ", Buttons)
                : "None";

            return StepType switch
            {
                MacroStepType.Tap => $"TAP [{btns}] ({DurationMs}ms, wait {DelayAfterMs}ms)",
                MacroStepType.PressDown => $"HOLD [{btns}] (wait {DelayAfterMs}ms)",
                MacroStepType.ReleaseUp => $"RELEASE [{btns}] (wait {DelayAfterMs}ms)",
                _ => $"[{btns}]"
            };
        }
    }

    public class MacroDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "New Macro";
        public string TriggerInput { get; set; } = "Unassigned";
        public MacroMode Mode { get; set; } = MacroMode.PlayOnce;
        public bool IsEnabled { get; set; } = true;
        public int LoopDelayMs { get; set; } = 20;
        public List<MacroStep> Steps { get; set; } = new();

        public MacroDefinition Clone()
        {
            var clone = new MacroDefinition
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = this.Name + " (Copy)",
                TriggerInput = this.TriggerInput,
                Mode = this.Mode,
                IsEnabled = this.IsEnabled,
                LoopDelayMs = this.LoopDelayMs,
                Steps = new List<MacroStep>()
            };

            if (this.Steps != null)
            {
                foreach (var step in this.Steps)
                {
                    clone.Steps.Add(step?.Clone() ?? new MacroStep());
                }
            }

            return clone;
        }
    }
}
