using System;
using System.Collections.Generic;

namespace VirtualController.Profiles
{
    public static class MacroPresets
    {
        /// <summary>
        /// Returns all 6 default macro presets.
        /// </summary>
        public static List<MacroDefinition> GetAllPresets()
        {
            return new List<MacroDefinition>
            {
                CreateRapidFireRT(),
                CreateUltimateCombo(),
                CreateMeleeCombo(),
                CreateDodgeRollCombo(),
                CreateBunnyHop(),
                CreateAutoSprintToggle()
            };
        }

        /// <summary>
        /// Creates the Rapid Fire RT (Turbo Shoot) preset.
        /// </summary>
        public static MacroDefinition CreateRapidFireRT()
        {
            return new MacroDefinition
            {
                Name = "Rapid Fire RT (Turbo Shoot)",
                TriggerInput = "V",
                Mode = MacroMode.RepeatWhileHeld,
                LoopDelayMs = 10,
                IsEnabled = true,
                Steps = new List<MacroStep>
                {
                    new MacroStep
                    {
                        StepType = MacroStepType.Tap,
                        Buttons = new List<string> { "RT" },
                        DurationMs = 35,
                        DelayAfterMs = 35
                    }
                }
            };
        }

        /// <summary>
        /// Creates the Ultimate Skill (LB + RB) preset.
        /// </summary>
        public static MacroDefinition CreateUltimateCombo()
        {
            return new MacroDefinition
            {
                Name = "Ultimate Skill (LB + RB)",
                TriggerInput = "Z",
                Mode = MacroMode.PlayOnce,
                IsEnabled = true,
                Steps = new List<MacroStep>
                {
                    new MacroStep
                    {
                        StepType = MacroStepType.Tap,
                        Buttons = new List<string> { "LB", "RB" },
                        DurationMs = 80,
                        DelayAfterMs = 40
                    }
                }
            };
        }

        /// <summary>
        /// Creates the Melee Combo (X -> X -> Y) preset.
        /// </summary>
        public static MacroDefinition CreateMeleeCombo()
        {
            return new MacroDefinition
            {
                Name = "Melee Combo (X -> X -> Y)",
                TriggerInput = "G",
                Mode = MacroMode.PlayOnce,
                IsEnabled = true,
                Steps = new List<MacroStep>
                {
                    new MacroStep
                    {
                        StepType = MacroStepType.Tap,
                        Buttons = new List<string> { "X" },
                        DurationMs = 60,
                        DelayAfterMs = 90
                    },
                    new MacroStep
                    {
                        StepType = MacroStepType.Tap,
                        Buttons = new List<string> { "X" },
                        DurationMs = 60,
                        DelayAfterMs = 90
                    },
                    new MacroStep
                    {
                        StepType = MacroStepType.Tap,
                        Buttons = new List<string> { "Y" },
                        DurationMs = 100,
                        DelayAfterMs = 120
                    }
                }
            };
        }

        /// <summary>
        /// Creates the Dodge Roll Forward (LS_Up + B) preset.
        /// </summary>
        public static MacroDefinition CreateDodgeRollCombo()
        {
            return new MacroDefinition
            {
                Name = "Dodge Roll Forward (LS_Up + B)",
                TriggerInput = "LeftAlt",
                Mode = MacroMode.PlayOnce,
                IsEnabled = true,
                Steps = new List<MacroStep>
                {
                    new MacroStep
                    {
                        StepType = MacroStepType.PressDown,
                        Buttons = new List<string> { "LS_Up" },
                        DurationMs = 0,
                        DelayAfterMs = 30
                    },
                    new MacroStep
                    {
                        StepType = MacroStepType.Tap,
                        Buttons = new List<string> { "B" },
                        DurationMs = 60,
                        DelayAfterMs = 60
                    },
                    new MacroStep
                    {
                        StepType = MacroStepType.ReleaseUp,
                        Buttons = new List<string> { "LS_Up" },
                        DurationMs = 0,
                        DelayAfterMs = 20
                    }
                }
            };
        }

        /// <summary>
        /// Creates the Auto Bunny Hop (Turbo A) preset.
        /// </summary>
        public static MacroDefinition CreateBunnyHop()
        {
            return new MacroDefinition
            {
                Name = "Auto Bunny Hop (Turbo A)",
                TriggerInput = "B",
                Mode = MacroMode.RepeatWhileHeld,
                LoopDelayMs = 20,
                IsEnabled = true,
                Steps = new List<MacroStep>
                {
                    new MacroStep
                    {
                        StepType = MacroStepType.Tap,
                        Buttons = new List<string> { "A" },
                        DurationMs = 45,
                        DelayAfterMs = 45
                    }
                }
            };
        }

        /// <summary>
        /// Creates the Auto Sprint Hold (LS) preset.
        /// </summary>
        public static MacroDefinition CreateAutoSprintToggle()
        {
            return new MacroDefinition
            {
                Name = "Auto Sprint Hold (LS)",
                TriggerInput = "CapsLock",
                Mode = MacroMode.ToggleLoop,
                LoopDelayMs = 0,
                IsEnabled = true,
                Steps = new List<MacroStep>
                {
                    new MacroStep
                    {
                        StepType = MacroStepType.Tap,
                        Buttons = new List<string> { "LS" },
                        DurationMs = 200,
                        DelayAfterMs = 0
                    }
                }
            };
        }

        /// <summary>
        /// Creates a macro preset by its key name.
        /// Supported keys: "RapidFireRT", "UltimateCombo", "MeleeCombo", "DodgeRoll", "BunnyHop", "AutoSprint".
        /// </summary>
        public static MacroDefinition? CreateByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return key.Trim().ToLowerInvariant() switch
            {
                "rapidfirert" or "rapid_fire_rt" => CreateRapidFireRT(),
                "ultimatecombo" or "ultimate_combo" => CreateUltimateCombo(),
                "meleecombo" or "melee_combo" => CreateMeleeCombo(),
                "dodgeroll" or "dodge_roll" => CreateDodgeRollCombo(),
                "bunnyhop" or "bunny_hop" => CreateBunnyHop(),
                "autosprint" or "auto_sprint" => CreateAutoSprintToggle(),
                _ => null
            };
        }
    }
}
