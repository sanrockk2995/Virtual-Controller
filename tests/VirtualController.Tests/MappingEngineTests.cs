using System;
using VirtualController.Core;
using VirtualController.Profiles;
using Xunit;

namespace VirtualController.Tests
{
    public class MappingEngineTests
    {
        [Fact]
        public void DefaultProfile_MapsAllButtonsCorrectly()
        {
            var engine = new MappingEngine();
            var input = new InputState();
            var profile = Profile.CreateDefaultProfile();

            // Test Space -> A
            input.SetKey("Space", true);
            var state = engine.Process(input, profile);
            Assert.True(state.A);
            Assert.False(state.B);

            // Test LeftShift -> B
            input.SetKey("LeftShift", true);
            state = engine.Process(input, profile);
            Assert.True(state.A);
            Assert.True(state.B);

            // Release Space
            input.SetKey("Space", false);
            state = engine.Process(input, profile);
            Assert.False(state.A);
            Assert.True(state.B);

            // Test E -> X, Q -> Y
            input.SetKey("E", true);
            input.SetKey("Q", true);
            state = engine.Process(input, profile);
            Assert.True(state.X);
            Assert.True(state.Y);

            // Test R -> RB, F -> LB
            input.SetKey("R", true);
            input.SetKey("F", true);
            state = engine.Process(input, profile);
            Assert.True(state.RightShoulder);
            Assert.True(state.LeftShoulder);

            // Test Tab -> Back, Enter -> Start
            input.SetKey("Tab", true);
            input.SetKey("Enter", true);
            state = engine.Process(input, profile);
            Assert.True(state.Back);
            Assert.True(state.Start);
        }

        [Fact]
        public void DefaultProfile_MouseButtonsMapToTriggersAndThumb()
        {
            var engine = new MappingEngine();
            var input = new InputState();
            var profile = Profile.CreateDefaultProfile();

            // LeftMouse -> RT (Right Trigger)
            input.SetMouseButton("LeftMouse", true);
            var state = engine.Process(input, profile);
            Assert.Equal(1.0, state.RightTrigger, 2);
            Assert.Equal(0.0, state.LeftTrigger, 2);

            // RightMouse -> LT (Left Trigger)
            input.SetMouseButton("RightMouse", true);
            state = engine.Process(input, profile);
            Assert.Equal(1.0, state.RightTrigger, 2);
            Assert.Equal(1.0, state.LeftTrigger, 2);

            // MiddleMouse -> RS (Right Thumb)
            input.SetMouseButton("MiddleMouse", true);
            state = engine.Process(input, profile);
            Assert.True(state.RightThumb);
        }

        [Fact]
        public void DefaultProfile_EscapeMapsToStart()
        {
            var engine = new MappingEngine();
            var input = new InputState();
            var profile = Profile.CreateDefaultProfile();

            input.SetKey("Escape", true);
            var state = engine.Process(input, profile);
            Assert.True(state.Start, "Escape key should trigger Start button in default profile");
        }
    }
}
