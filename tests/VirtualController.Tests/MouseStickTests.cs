using System;
using VirtualController.Core;
using VirtualController.Profiles;
using Xunit;

namespace VirtualController.Tests
{
    public class MouseStickTests
    {
        [Fact]
        public void MouseMovement_ProducesRightStickValues_WhenRightStickIsMouse()
        {
            var engine = new MappingEngine();
            var input = new InputState();
            var profile = Profile.CreateDefaultProfile();
            profile.RightStick.InputSource = "Mouse";
            profile.RightStick.Smoothing = 0.0; // Instant response for testing

            // Move mouse right (dx = 20) and up (dy = -20)
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

            // Swap: Left Stick = Mouse, Right Stick = Keyboard (Arrow keys)
            profile.LeftStick.InputSource = "Mouse";
            profile.LeftStick.Smoothing = 0.0;
            profile.RightStick.InputSource = "Keyboard";
            profile.RightStick.Up = "Up";
            profile.RightStick.Down = "Down";
            profile.RightStick.Left = "Left";
            profile.RightStick.Right = "Right";

            // 1. Move mouse right and up
            input.AddMouseDelta(20, -20);
            var state1 = engine.Process(input, profile);

            Assert.True(state1.LeftThumbX > 0, "Mouse movement should control LeftStick X when LeftStick is set to Mouse");
            Assert.True(state1.LeftThumbY > 0, "Mouse movement should control LeftStick Y when LeftStick is set to Mouse");
            Assert.Equal(0.0, state1.RightThumbX);
            Assert.Equal(0.0, state1.RightThumbY);

            // 2. Press Keyboard Right arrow -> should control RightStick X
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

            // Press W (LeftStick Up) and L (RightStick Right)
            input.SetKey("W", true);
            input.SetKey("L", true);

            var state = engine.Process(input, profile);

            Assert.Equal(0.0, state.LeftThumbX, 2);
            Assert.Equal(1.0, state.LeftThumbY, 2);

            Assert.Equal(1.0, state.RightThumbX, 2);
            Assert.Equal(0.0, state.RightThumbY, 2);
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
        public void MouseStationary_DecaysStickBackToCenter()
        {
            var engine = new MappingEngine();
            var input = new InputState();
            var profile = Profile.CreateDefaultProfile();
            profile.RightStick.Smoothing = 0.0;
            profile.RightStick.DecayRate = 0.50; // 50% decay per cycle

            // Move mouse to set a value
            input.AddMouseDelta(40, 0);
            var state1 = engine.Process(input, profile);
            double initialX = state1.RightThumbX;
            Assert.True(initialX > 0.1);

            // Now stationary (no mouse delta added)
            var state2 = engine.Process(input, profile);
            Assert.True(state2.RightThumbX < initialX, "Stick should decay towards 0 when mouse is stationary");

            // Several stationary ticks should reach 0
            for (int i = 0; i < 10; i++)
            {
                engine.Process(input, profile);
            }

            var finalState = engine.Process(input, profile);
            Assert.Equal(0.0, finalState.RightThumbX, 2);
            Assert.Equal(0.0, finalState.RightThumbY, 2);
        }

        [Fact]
        public void MaxMagnitude_ClampsExtremelyLargeDeltas()
        {
            var engine = new MappingEngine();
            var input = new InputState();
            var profile = Profile.CreateDefaultProfile();
            profile.RightStick.Smoothing = 0.0;
            profile.RightStick.MaxMagnitude = 1.0;

            // Huge mouse flick
            input.AddMouseDelta(5000, 5000);

            var state = engine.Process(input, profile);
            double mag = Math.Sqrt(state.RightThumbX * state.RightThumbX + state.RightThumbY * state.RightThumbY);

            Assert.True(mag <= 1.0001, $"Clamped magnitude should be <= 1.0, was {mag}");
        }
    }
}
