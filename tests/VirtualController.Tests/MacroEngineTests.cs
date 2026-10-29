using System;
using System.Collections.Generic;
using VirtualController.Core;
using VirtualController.Profiles;
using Xunit;

namespace VirtualController.Tests
{
    public class MacroEngineTests
    {
        [Fact]
        public void MacroEngine_PlayOnce_ExecutesSequenceAndStops()
        {
            var engine = new MacroEngine();
            var input = new InputState();
            var output = new ControllerState();

            var macro = new MacroDefinition
            {
                Id = "play_once_test",
                Name = "Play Once Test",
                TriggerInput = "F",
                Mode = MacroMode.PlayOnce,
                IsEnabled = true,
                Steps = new List<MacroStep>
                {
                    new MacroStep
                    {
                        StepType = MacroStepType.Tap,
                        Buttons = new List<string> { "X" },
                        DurationMs = 40,
                        DelayAfterMs = 20
                    },
                    new MacroStep
                    {
                        StepType = MacroStepType.Tap,
                        Buttons = new List<string> { "Y" },
                        DurationMs = 40,
                        DelayAfterMs = 20
                    }
                }
            };

            var macros = new List<MacroDefinition> { macro };

            // Trigger key pressed (rising edge)
            input.SetKey("F", true);

            // Step 1: Holding phase (dt = 20ms) -> X should be pressed
            output.Reset();
            engine.Update(input, macros, output, 0.020);
            Assert.True(engine.IsMacroRunning(macro.Id));
            Assert.True(output.X);
            Assert.False(output.Y);

            // Step 1: Completing holding phase (dt = 20ms, total 40ms) -> enters delay phase
            output.Reset();
            engine.Update(input, macros, output, 0.020);
            // In delay phase: X is released, Y not yet pressed
            Assert.False(output.X);
            Assert.False(output.Y);

            // Step 1: Delay phase finishes (dt = 20ms) -> advances to Step 2 holding start
            output.Reset();
            engine.Update(input, macros, output, 0.020);
            // Start of Step 2: Y should be pressed
            Assert.True(output.Y);
            Assert.False(output.X);

            // Step 2: Mid-holding phase (dt = 20ms, 20/40ms elapsed) -> Y remains pressed
            output.Reset();
            engine.Update(input, macros, output, 0.020);
            Assert.True(output.Y);
            Assert.False(output.X);

            // Step 2: Completing holding phase (dt = 20ms, 40/40ms) -> enters delay phase
            output.Reset();
            engine.Update(input, macros, output, 0.020);
            Assert.False(output.X);
            Assert.False(output.Y);

            // Step 2: Delay phase finishes (dt = 20ms) -> sequence completes and stops
            output.Reset();
            engine.Update(input, macros, output, 0.020);
            Assert.False(engine.IsMacroRunning(macro.Id));
            Assert.False(output.X);
            Assert.False(output.Y);

            // Verify macro stays stopped even if trigger remains held
            output.Reset();
            engine.Update(input, macros, output, 0.020);
            Assert.False(engine.IsMacroRunning(macro.Id));
            Assert.False(output.X);
            Assert.False(output.Y);
        }

        [Fact]
        public void MacroEngine_RepeatWhileHeld_LoopsWhileHeldAndReleasesImmediatelyOnKeyUp()
        {
            var engine = new MacroEngine();
            var input = new InputState();
            var output = new ControllerState();

            var macro = new MacroDefinition
            {
                Id = "turbo_rt",
                Name = "Turbo RT",
                TriggerInput = "V",
                Mode = MacroMode.RepeatWhileHeld,
                IsEnabled = true,
                LoopDelayMs = 10,
                Steps = new List<MacroStep>
                {
                    new MacroStep
                    {
                        StepType = MacroStepType.Tap,
                        Buttons = new List<string> { "RT" },
                        DurationMs = 30,
                        DelayAfterMs = 20
                    }
                }
            };
            var macros = new List<MacroDefinition> { macro };

            // 1. Press and hold trigger key
            input.SetKey("V", true);

            output.Reset();
            engine.Update(input, macros, output, 0.015);
            Assert.True(engine.IsMacroRunning(macro.Id));
            Assert.Equal(1.0, output.RightTrigger, 2);

            // 2. Advance mid-step while still holding
            output.Reset();
            engine.Update(input, macros, output, 0.010);
            Assert.True(engine.IsMacroRunning(macro.Id));
            Assert.Equal(1.0, output.RightTrigger, 2);

            // 3. Release trigger key mid-step
            input.SetKey("V", false);
            output.Reset();
            engine.Update(input, macros, output, 0.010);

            // Must immediately stop running and reset RightTrigger to 0.0 (zero stuck buttons)
            Assert.False(engine.IsMacroRunning(macro.Id));
            Assert.Equal(0.0, output.RightTrigger, 2);

            // 4. Subsequent updates while key is released stay stopped
            output.Reset();
            engine.Update(input, macros, output, 0.010);
            Assert.False(engine.IsMacroRunning(macro.Id));
            Assert.Equal(0.0, output.RightTrigger, 2);
        }

        [Fact]
        public void MacroEngine_ToggleLoop_StartsOnFirstPressAndStopsOnSecondPress()
        {
            var engine = new MacroEngine();
            var input = new InputState();
            var output = new ControllerState();

            var macro = new MacroDefinition
            {
                Id = "toggle_sprint",
                Name = "Toggle Sprint",
                TriggerInput = "CapsLock",
                Mode = MacroMode.ToggleLoop,
                IsEnabled = true,
                LoopDelayMs = 0,
                Steps = new List<MacroStep>
                {
                    new MacroStep
                    {
                        StepType = MacroStepType.Tap,
                        Buttons = new List<string> { "LS" },
                        DurationMs = 100,
                        DelayAfterMs = 0
                    }
                }
            };
            var macros = new List<MacroDefinition> { macro };

            // 1. First rising edge (Key Down)
            input.SetKey("CapsLock", true);
            output.Reset();
            engine.Update(input, macros, output, 0.020);
            Assert.True(engine.IsMacroRunning(macro.Id));
            Assert.True(output.LeftThumb);

            // 2. Key Up (falling edge) -> macro continues running in ToggleLoop mode
            input.SetKey("CapsLock", false);
            output.Reset();
            engine.Update(input, macros, output, 0.020);
            Assert.True(engine.IsMacroRunning(macro.Id));
            Assert.True(output.LeftThumb);

            // Advance across loop boundary
            output.Reset();
            engine.Update(input, macros, output, 0.100);
            Assert.True(engine.IsMacroRunning(macro.Id));

            // 3. Second rising edge (Key Down again) -> toggles off and stops
            input.SetKey("CapsLock", true);
            output.Reset();
            engine.Update(input, macros, output, 0.020);
            Assert.False(engine.IsMacroRunning(macro.Id));
            Assert.False(output.LeftThumb);

            // 4. Key Up after stopping -> stays stopped
            input.SetKey("CapsLock", false);
            output.Reset();
            engine.Update(input, macros, output, 0.020);
            Assert.False(engine.IsMacroRunning(macro.Id));
            Assert.False(output.LeftThumb);
        }

        [Fact]
        public void MacroEngine_PressDownAndReleaseUp_MaintainsHeldButtonAcrossSteps()
        {
            var engine = new MacroEngine();
            var input = new InputState();
            var output = new ControllerState();

            var macro = new MacroDefinition
            {
                Id = "combo_hold",
                Name = "Hold LB then Tap RB then Release LB",
                TriggerInput = "Z",
                Mode = MacroMode.PlayOnce,
                IsEnabled = true,
                Steps = new List<MacroStep>
                {
                    new MacroStep
                    {
                        StepType = MacroStepType.PressDown,
                        Buttons = new List<string> { "LB" },
                        DurationMs = 0,
                        DelayAfterMs = 30
                    },
                    new MacroStep
                    {
                        StepType = MacroStepType.Tap,
                        Buttons = new List<string> { "RB" },
                        DurationMs = 50,
                        DelayAfterMs = 20
                    },
                    new MacroStep
                    {
                        StepType = MacroStepType.ReleaseUp,
                        Buttons = new List<string> { "LB" },
                        DurationMs = 0,
                        DelayAfterMs = 10
                    }
                }
            };
            var macros = new List<MacroDefinition> { macro };

            input.SetKey("Z", true);

            // Step 1: PressDown LB (dt = 15ms < 30ms delay)
            output.Reset();
            engine.Update(input, macros, output, 0.015);
            Assert.True(engine.IsMacroRunning(macro.Id));
            Assert.True(output.LeftShoulder);
            Assert.False(output.RightShoulder);

            // Complete Step 1 delay (dt = 15ms) -> advances to Step 2
            output.Reset();
            engine.Update(input, macros, output, 0.015);

            // Step 2: Tap RB while LB remains held from Step 1 (dt = 25ms < 50ms duration)
            output.Reset();
            engine.Update(input, macros, output, 0.025);
            // Verifies simultaneous LeftShoulder && RightShoulder during Step 2
            Assert.True(output.LeftShoulder, "LeftShoulder should remain held from Step 1");
            Assert.True(output.RightShoulder, "RightShoulder should be active during Step 2 tap");

            // Complete Step 2 holding phase (dt = 25ms) -> enters Step 2 delay phase (20ms)
            output.Reset();
            engine.Update(input, macros, output, 0.025);
            // In Step 2 delay: RB tap ended, but LB is still held
            Assert.True(output.LeftShoulder);
            Assert.False(output.RightShoulder);

            // Complete Step 2 delay and Step 3 ReleaseUp (dt = 30ms)
            output.Reset();
            engine.Update(input, macros, output, 0.030);

            // Advance past Step 3 delay to macro completion
            output.Reset();
            engine.Update(input, macros, output, 0.020);
            Assert.False(engine.IsMacroRunning(macro.Id));
            Assert.False(output.LeftShoulder);
            Assert.False(output.RightShoulder);
        }

        [Fact]
        public void MacroEngine_MultiButtonComboAndStickDirections_AppliesCorrectly()
        {
            var engine = new MacroEngine();
            var input = new InputState();
            var output = new ControllerState();

            var macro = new MacroDefinition
            {
                Id = "multi_combo",
                Name = "Multi Combo",
                TriggerInput = "Q",
                Mode = MacroMode.PlayOnce,
                IsEnabled = true,
                Steps = new List<MacroStep>
                {
                    new MacroStep
                    {
                        StepType = MacroStepType.Tap,
                        Buttons = new List<string> { "A", "LS_Up", "RS_Right" },
                        DurationMs = 50,
                        DelayAfterMs = 20
                    }
                }
            };
            var macros = new List<MacroDefinition> { macro };

            input.SetKey("Q", true);
            output.Reset();
            engine.Update(input, macros, output, 0.020);

            Assert.True(output.A);
            Assert.Equal(1.0, output.LeftThumbY, 2);
            Assert.Equal(1.0, output.RightThumbX, 2);

            // Also verify all stick direction mappings in ApplyVirtualAction
            var testState = new ControllerState();
            MacroEngine.ApplyVirtualAction("LS_Down", testState);
            Assert.Equal(-1.0, testState.LeftThumbY, 2);

            MacroEngine.ApplyVirtualAction("LS_Left", testState);
            Assert.Equal(-1.0, testState.LeftThumbX, 2);

            MacroEngine.ApplyVirtualAction("RS_Up", testState);
            Assert.Equal(1.0, testState.RightThumbY, 2);

            MacroEngine.ApplyVirtualAction("RS_Down", testState);
            Assert.Equal(-1.0, testState.RightThumbY, 2);

            MacroEngine.ApplyVirtualAction("RS_Left", testState);
            Assert.Equal(-1.0, testState.RightThumbX, 2);
        }

        [Fact]
        public void MappingEngine_ProcessWithProfileMacros_IntegratesEndToEnd()
        {
            var mappingEngine = new MappingEngine();
            var profile = Profile.CreateDefaultProfile();
            // Default profile has "Space" -> "A"
            var macro = new MacroDefinition
            {
                Id = "macro_rt",
                Name = "Turbo RT",
                TriggerInput = "V",
                Mode = MacroMode.RepeatWhileHeld,
                IsEnabled = true,
                Steps = new List<MacroStep>
                {
                    new MacroStep
                    {
                        StepType = MacroStepType.Tap,
                        Buttons = new List<string> { "RT" },
                        DurationMs = 50,
                        DelayAfterMs = 50
                    }
                }
            };
            profile.Macros.Add(macro);

            var input = new InputState();
            // Press normal mapped key "Space" and macro trigger "V" simultaneously
            input.SetKey("Space", true);
            input.SetKey("V", true);

            var state = mappingEngine.Process(input, profile, explicitDt: 0.020);

            // Both normal mapped button (A) and macro output (RT) are active
            Assert.True(state.A, "Normal button mapping should be active");
            Assert.Equal(1.0, state.RightTrigger, 2);
            Assert.True(mappingEngine.MacroEngine.IsMacroRunning(macro.Id));

            // Calling MappingEngine.Reset() clears all macro engine runtime states
            mappingEngine.Reset();
            Assert.False(mappingEngine.MacroEngine.IsMacroRunning(macro.Id));
        }

        [Fact]
        public void Profile_GetMappedPhysicalKeyboardKeys_IncludesEnabledMacroTriggersOnly()
        {
            var profile = new Profile
            {
                Buttons = new Dictionary<string, string>(),
                ExtraMappings = new Dictionary<string, string>(),
                LeftStick = StickConfig.CreateDefaultLeft(),
                RightStick = StickConfig.CreateDefaultRight()
            };
            // Disable sticks so they don't add keyboard keys for this test
            profile.LeftStick.InputSource = "None";
            profile.RightStick.InputSource = "None";

            profile.Macros = new List<MacroDefinition>
            {
                new MacroDefinition { Id = "m1", TriggerInput = "V", IsEnabled = true },
                new MacroDefinition { Id = "m2", TriggerInput = "G", IsEnabled = true },
                new MacroDefinition { Id = "m3", TriggerInput = "H", IsEnabled = false }, // Disabled
                new MacroDefinition { Id = "m4", TriggerInput = "XButton1", IsEnabled = true }, // Mouse button
                new MacroDefinition { Id = "m5", TriggerInput = "RightMouse", IsEnabled = true }, // Mouse button
                new MacroDefinition { Id = "m6", TriggerInput = "Unassigned", IsEnabled = true }, // Unassigned
                new MacroDefinition { Id = "m7", TriggerInput = "", IsEnabled = true } // Empty
            };

            var keys = profile.GetMappedPhysicalKeyboardKeys();

            Assert.Contains("V", keys);
            Assert.Contains("G", keys);
            Assert.DoesNotContain("H", keys);
            Assert.DoesNotContain("XButton1", keys);
            Assert.DoesNotContain("RightMouse", keys);
            Assert.DoesNotContain("Unassigned", keys);
            Assert.Equal(2, keys.Count);
        }

        [Fact]
        public void MacroEngine_DisabledOrUnassignedMacro_DoesNotExecute()
        {
            var engine = new MacroEngine();
            var input = new InputState();
            var output = new ControllerState();

            var disabledMacro = new MacroDefinition
            {
                Id = "disabled_macro",
                TriggerInput = "K",
                IsEnabled = false,
                Steps = new List<MacroStep>
                {
                    new MacroStep { Buttons = new List<string> { "A" } }
                }
            };

            var unassignedMacro = new MacroDefinition
            {
                Id = "unassigned_macro",
                TriggerInput = "Unassigned",
                IsEnabled = true,
                Steps = new List<MacroStep>
                {
                    new MacroStep { Buttons = new List<string> { "B" } }
                }
            };

            var macros = new List<MacroDefinition> { disabledMacro, unassignedMacro };

            input.SetKey("K", true);
            engine.Update(input, macros, output, 0.020);

            Assert.False(engine.IsMacroRunning("disabled_macro"));
            Assert.False(engine.IsMacroRunning("unassigned_macro"));
            Assert.False(output.A);
            Assert.False(output.B);
        }

        [Fact]
        public void MacroEngine_StopMacro_StopsRunningMacroImmediately()
        {
            var engine = new MacroEngine();
            var input = new InputState();
            var output = new ControllerState();

            var macro = new MacroDefinition
            {
                Id = "stop_test",
                TriggerInput = "T",
                Mode = MacroMode.PlayOnce,
                IsEnabled = true,
                Steps = new List<MacroStep>
                {
                    new MacroStep { Buttons = new List<string> { "X" }, DurationMs = 200 }
                }
            };

            input.SetKey("T", true);
            engine.Update(input, new List<MacroDefinition> { macro }, output, 0.020);
            Assert.True(engine.IsMacroRunning(macro.Id));

            engine.StopMacro(macro.Id);
            Assert.False(engine.IsMacroRunning(macro.Id));
        }

        [Fact]
        public void MacroDefinition_Clone_CreatesDeepCopy()
        {
            var original = new MacroDefinition
            {
                Id = "orig_id",
                Name = "Original",
                TriggerInput = "J",
                Mode = MacroMode.RepeatWhileHeld,
                IsEnabled = true,
                LoopDelayMs = 30,
                Steps = new List<MacroStep>
                {
                    new MacroStep
                    {
                        StepType = MacroStepType.Tap,
                        Buttons = new List<string> { "A", "B" },
                        DurationMs = 50,
                        DelayAfterMs = 40
                    }
                }
            };

            var clone = original.Clone();

            Assert.NotEqual(original.Id, clone.Id);
            Assert.Equal("Original (Copy)", clone.Name);
            Assert.Equal(original.TriggerInput, clone.TriggerInput);
            Assert.Equal(original.Mode, clone.Mode);
            Assert.Equal(original.IsEnabled, clone.IsEnabled);
            Assert.Equal(original.LoopDelayMs, clone.LoopDelayMs);
            Assert.Single(clone.Steps);
            Assert.NotSame(original.Steps[0], clone.Steps[0]);
            Assert.Equal(original.Steps[0].DurationMs, clone.Steps[0].DurationMs);
            Assert.Equal(original.Steps[0].Buttons, clone.Steps[0].Buttons);
        }

        [Fact]
        public void MacroStep_GetDisplaySummary_FormatsCorrectly()
        {
            var tapStep = new MacroStep
            {
                StepType = MacroStepType.Tap,
                Buttons = new List<string> { "LB", "RB" },
                DurationMs = 80,
                DelayAfterMs = 40
            };
            Assert.Contains("TAP [LB + RB]", tapStep.GetDisplaySummary());

            var waitStep = new MacroStep
            {
                StepType = MacroStepType.Wait,
                DelayAfterMs = 150
            };
            Assert.Contains("WAIT (150 ms)", waitStep.GetDisplaySummary());

            var holdStep = new MacroStep
            {
                StepType = MacroStepType.PressDown,
                Buttons = new List<string> { "LS_Up" },
                DelayAfterMs = 30
            };
            Assert.Contains("HOLD [LS_Up]", holdStep.GetDisplaySummary());

            var releaseStep = new MacroStep
            {
                StepType = MacroStepType.ReleaseUp,
                Buttons = new List<string> { "LS_Up" },
                DelayAfterMs = 20
            };
            Assert.Contains("RELEASE [LS_Up]", releaseStep.GetDisplaySummary());
        }
    }
}
