using System;
using System.Collections.Generic;
using VirtualController.Core;
using VirtualController.Profiles;
using Xunit;

namespace VirtualController.Tests
{
    public class MacroRecorderTests
    {
        [Fact]
        public void MacroRecorder_RecordsRealTimeButtonDownAndUpWithAccurateDurationAndDelay()
        {
            var recorder = new MacroRecorder();
            recorder.Start(explicitTimestampMs: 0);

            // Button A down at 100ms, up at 160ms (duration = 60ms)
            recorder.RecordButtonDown("A", explicitTimestampMs: 100);
            recorder.RecordButtonUp("A", explicitTimestampMs: 160);

            // Button B down at 250ms, up at 330ms (duration = 80ms, delay after step 1 = 250 - 160 = 90ms)
            recorder.RecordButtonDown("B", explicitTimestampMs: 250);
            recorder.RecordButtonUp("B", explicitTimestampMs: 330);

            var steps = recorder.Stop(explicitTimestampMs: 400);

            Assert.Equal(2, steps.Count);

            Assert.Equal("A", steps[0].Buttons[0]);
            Assert.Equal(60, steps[0].DurationMs);
            Assert.Equal(90, steps[0].DelayAfterMs);
            Assert.Equal(MacroStepType.Tap, steps[0].StepType);

            Assert.Equal("B", steps[1].Buttons[0]);
            Assert.Equal(80, steps[1].DurationMs);
            Assert.Equal(MacroStepType.Tap, steps[1].StepType);
        }

        [Fact]
        public void MacroRecorder_UseFixedTiming_UsesConfiguredFixedValues()
        {
            var recorder = new MacroRecorder
            {
                UseFixedTiming = true,
                FixedDurationMs = 70,
                FixedDelayAfterMs = 45
            };

            recorder.Start(explicitTimestampMs: 0);

            // Real elapsed durations: 200ms and 150ms
            recorder.RecordButtonDown("X", explicitTimestampMs: 100);
            recorder.RecordButtonUp("X", explicitTimestampMs: 300);

            recorder.RecordButtonDown("Y", explicitTimestampMs: 500);
            recorder.RecordButtonUp("Y", explicitTimestampMs: 650);

            var steps = recorder.Stop(explicitTimestampMs: 700);

            Assert.Equal(2, steps.Count);
            Assert.Equal(70, steps[0].DurationMs);
            Assert.Equal(45, steps[0].DelayAfterMs);

            Assert.Equal(70, steps[1].DurationMs);
            Assert.Equal(45, steps[1].DelayAfterMs);
        }

        [Fact]
        public void MacroRecorder_IgnoresKeyRepeatAndFinalizesHeldButtonsOnStop()
        {
            var recorder = new MacroRecorder();
            recorder.Start(explicitTimestampMs: 0);

            // Simulate key repeat while held: multiple down events
            recorder.RecordButtonDown("X", explicitTimestampMs: 100);
            recorder.RecordButtonDown("X", explicitTimestampMs: 130);
            recorder.RecordButtonDown("X", explicitTimestampMs: 160);

            // Stop while still held at 250ms -> should finalize the single held button
            var steps = recorder.Stop(explicitTimestampMs: 250);

            Assert.Single(steps);
            Assert.Equal("X", steps[0].Buttons[0]);
            Assert.Equal(150, steps[0].DurationMs); // 250 - 100 = 150ms
            Assert.False(recorder.IsRecording);
        }

        [Fact]
        public void MacroPresets_GetAllPresets_ReturnsValidNonEmptyPresets()
        {
            var presets = MacroPresets.GetAllPresets();

            Assert.NotNull(presets);
            Assert.Equal(6, presets.Count);

            foreach (var preset in presets)
            {
                Assert.False(string.IsNullOrWhiteSpace(preset.Name));
                Assert.False(string.IsNullOrWhiteSpace(preset.TriggerInput));
                Assert.True(preset.IsEnabled);
                Assert.NotNull(preset.Steps);
                Assert.NotEmpty(preset.Steps);
                foreach (var step in preset.Steps)
                {
                    Assert.NotEmpty(step.Buttons);
                }
            }

            // Verify CreateByKey for all 6 presets (both PascalCase and snake_case)
            var rapidFire = MacroPresets.CreateByKey("RapidFireRT");
            Assert.NotNull(rapidFire);
            Assert.Equal("Rapid Fire RT (Turbo Shoot)", rapidFire.Name);
            Assert.Equal(MacroMode.RepeatWhileHeld, rapidFire.Mode);

            var ultimate = MacroPresets.CreateByKey("ultimate_combo");
            Assert.NotNull(ultimate);
            Assert.Equal("Ultimate Skill (LB + RB)", ultimate.Name);

            var melee = MacroPresets.CreateByKey("MeleeCombo");
            Assert.NotNull(melee);
            Assert.Equal("Melee Combo (X -> X -> Y)", melee.Name);
            Assert.Equal(3, melee.Steps.Count);

            var dodgeRoll = MacroPresets.CreateByKey("dodge_roll");
            Assert.NotNull(dodgeRoll);
            Assert.Equal("Dodge Roll Forward (LS_Up + B)", dodgeRoll.Name);

            var bunnyHop = MacroPresets.CreateByKey("BunnyHop");
            Assert.NotNull(bunnyHop);
            Assert.Equal("Auto Bunny Hop (Turbo A)", bunnyHop.Name);
            Assert.Equal(MacroMode.RepeatWhileHeld, bunnyHop.Mode);

            var autoSprint = MacroPresets.CreateByKey("auto_sprint");
            Assert.NotNull(autoSprint);
            Assert.Equal("Auto Sprint Hold (LS)", autoSprint.Name);
            Assert.Equal(MacroMode.ToggleLoop, autoSprint.Mode);

            // Invalid and empty keys
            Assert.Null(MacroPresets.CreateByKey("nonexistent"));
            Assert.Null(MacroPresets.CreateByKey(""));
            Assert.Null(MacroPresets.CreateByKey(null!));
        }

        [Fact]
        public void MacroRecorder_Cancel_ClearsRecordedStepsAndState()
        {
            var recorder = new MacroRecorder();
            recorder.Start(explicitTimestampMs: 0);

            recorder.RecordButtonDown("A", explicitTimestampMs: 50);
            recorder.RecordButtonUp("A", explicitTimestampMs: 120);

            recorder.Cancel();

            Assert.False(recorder.IsRecording);
            var steps = recorder.Stop(explicitTimestampMs: 200);
            Assert.Empty(steps);
        }

        [Fact]
        public void MacroRecorder_ClampsShortDurationsToMinDuration()
        {
            var recorder = new MacroRecorder
            {
                MinDurationMs = 25
            };
            recorder.Start(explicitTimestampMs: 0);

            // 5ms tap should be clamped to MinDurationMs (25ms)
            recorder.RecordButtonDown("B", explicitTimestampMs: 100);
            recorder.RecordButtonUp("B", explicitTimestampMs: 105);

            var steps = recorder.Stop(explicitTimestampMs: 200);

            Assert.Single(steps);
            Assert.Equal(25, steps[0].DurationMs);
        }
    }
}
