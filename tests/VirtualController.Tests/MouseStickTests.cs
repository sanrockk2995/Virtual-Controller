using System;
using VirtualController.Core;
using VirtualController.Profiles;
using Xunit;

namespace VirtualController.Tests
{
    public class MouseStickTests
    {
        [Fact]
        public void ZeroMouseInput_ProducesNeutralStick()
        {
            var translator = new MouseToStickTranslator();
            var config = StickConfig.CreateDefaultRight();
            config.DecayRate = 0.0;

            var (x, y) = translator.Process(0, 0, config);

            Assert.Equal(0.0, x, 3);
            Assert.Equal(0.0, y, 3);
        }

        [Fact]
        public void SmallMovement_JumpsAboveAntiDeadzone()
        {
            var translator = new MouseToStickTranslator();
            var config = StickConfig.CreateDefaultRight();
            config.Smoothing = 0.0;
            config.AntiDeadzone = 0.20; // 20% anti-deadzone
            config.Deadzone = 0.01;

            // Small movement (e.g. 2 mickeys)
            var (x, y) = translator.Process(2, 0, config);

            // Output should immediately be at or above anti-deadzone
            Assert.True(x >= 0.20, $"Expected output >= AntiDeadzone (0.20), but was {x}");
            Assert.Equal(0.0, y, 3);
        }

        [Fact]
        public void LargeMovement_ClampsToMaxDeflection()
        {
            var translator = new MouseToStickTranslator();
            var config = StickConfig.CreateDefaultRight();
            config.Smoothing = 0.0;
            config.MaxDeflection = 0.85;

            // Very large flick
            var (x, y) = translator.Process(500, 0, config);

            Assert.Equal(0.85, x, 2);
            Assert.Equal(0.0, y, 2);
        }

        [Fact]
        public void IndependentSensitivity_ScalesAxesDifferently()
        {
            var translator = new MouseToStickTranslator();
            var config = StickConfig.CreateDefaultRight();
            config.Smoothing = 0.0;
            config.AntiDeadzone = 0.0;
            config.Deadzone = 0.0;
            config.ResponseCurve = "Linear";

            // X = 1.0, Y = 2.0 (vertical twice as sensitive)
            config.SensitivityX = 1.0;
            config.SensitivityY = 2.0;

            // Same input in X and Y
            var (x, y) = translator.Process(10, -10, config);

            Assert.True(y > x, $"Y output ({y}) should be greater than X output ({x}) due to SensitivityY = 2.0");
            Assert.Equal(2.0, y / x, 1);
        }

        [Fact]
        public void DiagonalMovement_RadialNormalizationPreservesMagnitude()
        {
            var translator = new MouseToStickTranslator();
            var config = StickConfig.CreateDefaultRight();
            config.Smoothing = 0.0;
            config.IsRadial = true;
            config.MaxDeflection = 1.0;

            // Equal diagonal movement
            var (x, y) = translator.Process(200, -200, config);

            double magnitude = Math.Sqrt(x * x + y * y);
            Assert.True(magnitude <= 1.0001, $"Radial magnitude {magnitude} should not exceed 1.0");
            Assert.Equal(Math.Sqrt(0.5), x, 2); // ~0.707
            Assert.Equal(Math.Sqrt(0.5), y, 2);
        }

        [Fact]
        public void DiagonalMovement_SquaredStickAllowsFullCorners()
        {
            var translator = new MouseToStickTranslator();
            var config = StickConfig.CreateDefaultRight();
            config.Smoothing = 0.0;
            config.IsRadial = false; // Square stick
            config.MaxDeflection = 1.0;

            // Very large diagonal movement
            var (x, y) = translator.Process(500, -500, config);

            // In square stick mode, each axis can independently saturate to 1.0
            Assert.Equal(1.0, x, 2);
            Assert.Equal(1.0, y, 2);
        }

        [Theory]
        [InlineData("Linear", 0.5, 0.5)]
        [InlineData("Delay", 0.5, 0.25)] // 0.5^2 = 0.25
        [InlineData("Aggressive", 0.25, 0.5)] // sqrt(0.25) = 0.5
        [InlineData("Smooth", 0.5, 0.5)] // 3*(0.25) - 2*(0.125) = 0.5
        public void ResponseCurves_EvaluateCorrectly(string curve, double input, double expectedOutput)
        {
            double result = MouseToStickTranslator.EvaluateCurve(input, curve);
            Assert.Equal(expectedOutput, result, 2);
        }

        [Fact]
        public void SuddenStop_ReturnsToNeutralImmediatelyWhenDecayIsZero()
        {
            var translator = new MouseToStickTranslator();
            var config = StickConfig.CreateDefaultRight();
            config.Smoothing = 0.0;
            config.DecayRate = 0.0; // Snappy return to center

            // 1. Mouse moving
            translator.Process(30, 0, config);
            Assert.True(translator.CurrentX > 0.0);

            // 2. Mouse suddenly stops (0 delta in next tick)
            var (stopX, stopY) = translator.Process(0, 0, config);

            Assert.Equal(0.0, stopX, 3);
            Assert.Equal(0.0, stopY, 3);
        }

        [Fact]
        public void SuddenDirectionReversal_FlipsSignInstantlyWithoutHysteresis()
        {
            var translator = new MouseToStickTranslator();
            var config = StickConfig.CreateDefaultRight();
            config.Smoothing = 0.0;
            config.DecayRate = 0.0;

            // 1. Move Right
            var (rightX, _) = translator.Process(30, 0, config);
            Assert.True(rightX > 0);

            // 2. Immediately reverse Left
            var (leftX, _) = translator.Process(-30, 0, config);
            Assert.True(leftX < 0, "Direction reversal should immediately produce negative deflection");
            Assert.Equal(-rightX, leftX, 2);
        }

        [Fact]
        public void InvertX_And_InvertY_WorkCorrectly()
        {
            var engine = new MappingEngine();
            var input = new InputState();
            var profile = Profile.CreateDefaultProfile();
            profile.RightStick.Smoothing = 0.0;
            profile.RightStick.InvertX = true;
            profile.RightStick.InvertY = true;

            input.AddMouseDelta(20, -20);

            var state = engine.Process(input, profile);

            Assert.True(state.RightThumbX < 0, "Inverted X should produce negative stick X for positive mouse delta");
            Assert.True(state.RightThumbY < 0, "Inverted Y should produce negative stick Y for upward mouse movement");
        }

        [Fact]
        public void MouseMovement_ProducesRightStickValues_WhenRightStickIsMouse()
        {
            var engine = new MappingEngine();
            var input = new InputState();
            var profile = Profile.CreateDefaultProfile();
            profile.RightStick.InputSource = "Mouse";
            profile.RightStick.Smoothing = 0.0;

            input.AddMouseDelta(20, -20);
            var state = engine.Process(input, profile);

            Assert.True(state.RightThumbX > 0, "Right mouse movement should produce positive RightThumbX");
            Assert.True(state.RightThumbY > 0, "Upward mouse movement should produce positive RightThumbY");

            double mag = Math.Sqrt(state.RightThumbX * state.RightThumbX + state.RightThumbY * state.RightThumbY);
            Assert.True(mag <= 1.0 + 1e-6, $"Right stick magnitude {mag} exceeded 1.0");
        }

        [Fact]
        public void MouseMovement_ProducesLeftStickValues_WhenLeftStickIsConfiguredAsMouse()
        {
            var engine = new MappingEngine();
            var input = new InputState();
            var profile = Profile.CreateDefaultProfile();

            profile.LeftStick.InputSource = "Mouse";
            profile.LeftStick.Smoothing = 0.0;
            profile.RightStick.InputSource = "Keyboard";
            profile.RightStick.Up = "Up";
            profile.RightStick.Down = "Down";
            profile.RightStick.Left = "Left";
            profile.RightStick.Right = "Right";

            input.AddMouseDelta(20, -20);
            var state1 = engine.Process(input, profile);

            Assert.True(state1.LeftThumbX > 0);
            Assert.True(state1.LeftThumbY > 0);
            Assert.Equal(0.0, state1.RightThumbX);
            Assert.Equal(0.0, state1.RightThumbY);

            input.SetKey("Right", true);
            var state2 = engine.Process(input, profile);

            Assert.Equal(1.0, state2.RightThumbX, 2);
            Assert.Equal(0.0, state2.RightThumbY, 2);
        }

        [Fact]
        public void BothSticks_CanBeConfiguredAsKeyboardSimultaneously()
        {
            var engine = new MappingEngine();
            var input = new InputState();
            var profile = new Profile
            {
                LeftStick = new StickConfig
                {
                    InputSource = "Keyboard",
                    Up = "W", Down = "S", Left = "A", Right = "D"
                },
                RightStick = new StickConfig
                {
                    InputSource = "Keyboard",
                    Up = "I", Down = "K", Left = "J", Right = "L"
                }
            };

            input.SetKey("W", true);
            input.SetKey("L", true);

            var state = engine.Process(input, profile);

            Assert.Equal(0.0, state.LeftThumbX, 2);
            Assert.Equal(1.0, state.LeftThumbY, 2);
            Assert.Equal(1.0, state.RightThumbX, 2);
            Assert.Equal(0.0, state.RightThumbY, 2);
        }

        [Fact]
        public void MaxMagnitude_ClampsExtremelyLargeDeltas()
        {
            var engine = new MappingEngine();
            var input = new InputState();
            var profile = Profile.CreateDefaultProfile();
            profile.RightStick.Smoothing = 0.0;
            profile.RightStick.MaxMagnitude = 1.0;

            input.AddMouseDelta(5000, 5000);

            var state = engine.Process(input, profile);
            double mag = Math.Sqrt(state.RightThumbX * state.RightThumbX + state.RightThumbY * state.RightThumbY);

            Assert.True(mag <= 1.0001, $"Clamped magnitude should be <= 1.0, was {mag}");
        }

        [Fact]
        public void NoiseRejection_SmallMickeysBelowDeadzone_YieldZeroOutput()
        {
            var translator = new MouseToStickTranslator();
            var config = StickConfig.CreateDefaultRight();
            config.Smoothing = 0.0;
            config.Deadzone = 0.10; // 10% deadzone threshold
            config.Sensitivity = 1.0;
            config.SensitivityX = 1.0;

            // 1 mickey: scaled magnitude = 1 * 1.0 * 0.05 = 0.05, which is < 0.10 deadzone
            var (x, y) = translator.Process(1, 0, config);

            Assert.Equal(0.0, x);
            Assert.Equal(0.0, y);
        }

        [Fact]
        public void Smoothing_WhenDisabled_ImmediatelyUpdatesToTarget()
        {
            var translator1 = new MouseToStickTranslator();
            var translator2 = new MouseToStickTranslator();
            var config = StickConfig.CreateDefaultRight();
            config.Smoothing = 0.0;
            config.AntiDeadzone = 0.0;
            config.Deadzone = 0.0;
            config.ResponseCurve = "Linear";

            // translator1 processed large delta in previous tick, then smaller delta in next tick
            translator1.Process(25, 0, config);
            var (x1, _) = translator1.Process(8, 0, config);

            // translator2 processes smaller delta directly without prior history
            var (x2, _) = translator2.Process(8, 0, config);

            // Without smoothing, output is strictly memoryless and matches fresh target immediately
            Assert.Equal(x2, x1, 4);
        }

        [Fact]
        public void Smoothing_WhenEnabled_LagsBehindTarget()
        {
            var translatorUnsmoothed = new MouseToStickTranslator();
            var translatorSmoothed = new MouseToStickTranslator();

            var configNoSmooth = StickConfig.CreateDefaultRight();
            configNoSmooth.Smoothing = 0.0;
            configNoSmooth.AntiDeadzone = 0.0;
            configNoSmooth.Deadzone = 0.0;

            var configSmoothed = StickConfig.CreateDefaultRight();
            configSmoothed.Smoothing = 0.8; // High smoothing
            configSmoothed.AntiDeadzone = 0.0;
            configSmoothed.Deadzone = 0.0;

            var (targetX, _) = translatorUnsmoothed.Process(30, 0, configNoSmooth);
            var (smoothedX, _) = translatorSmoothed.Process(30, 0, configSmoothed);

            // Smoothed output must lag behind the target
            Assert.True(smoothedX < targetX, $"Smoothed output ({smoothedX}) should be less than immediate target ({targetX})");
            Assert.True(smoothedX > 0.0, "Smoothed output should still move in the target direction");
        }

        [Fact]
        public void Process_WithVariableDeltaTime_CalculatesVelocityCorrectly()
        {
            var translator1 = new MouseToStickTranslator();
            var translator2 = new MouseToStickTranslator();

            var config = StickConfig.CreateDefaultRight();
            config.Smoothing = 0.0;
            config.AntiDeadzone = 0.0;
            config.Deadzone = 0.0;

            // 10 mickeys in 8ms (dt = 0.008s)
            var (x1, _) = translator1.Process(10, 0, config, dt: 0.008);

            // 20 mickeys in 16ms (dt = 0.016s) -> identical physical velocity (1.25 mickeys/ms)
            var (x2, _) = translator2.Process(20, 0, config, dt: 0.016);

            Assert.Equal(x1, x2, 3);
        }

        [Fact]
        public void AntiDeadzone_WhenDisabled_YieldsProportionalSmallOutput()
        {
            var translator = new MouseToStickTranslator();
            var config = StickConfig.CreateDefaultRight();
            config.Smoothing = 0.0;
            config.AntiDeadzone = 0.0; // Disabled
            config.Deadzone = 0.0;
            config.ResponseCurve = "Linear";

            var (x, y) = translator.Process(2, 0, config);

            // Should be a small value proportional to 2 mickeys (0.10), not jumped to an artificial offset
            Assert.True(x > 0.0 && x < 0.20, $"Expected small proportional output, but got {x}");
            Assert.Equal(0.0, y);
        }

        [Fact]
        public void TranslatorReset_ClearsAllStateToNeutral()
        {
            var translator = new MouseToStickTranslator();
            var config = StickConfig.CreateDefaultRight();
            config.Smoothing = 0.8;

            // Move mouse
            translator.Process(40, -40, config);
            Assert.True(translator.CurrentX != 0.0);
            Assert.True(translator.CurrentY != 0.0);

            // Reset
            translator.Reset();
            Assert.Equal(0.0, translator.CurrentX);
            Assert.Equal(0.0, translator.CurrentY);
        }
    }
}
