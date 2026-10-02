using System;
using VirtualController.Core;
using VirtualController.Backend;
using Xunit;

namespace VirtualController.Tests
{
    public class SafetyTests
    {
        [Fact]
        public void ControllerState_Reset_NeutralizesAllFields()
        {
            var state = new ControllerState
            {
                A = true,
                B = true,
                X = true,
                Y = true,
                LeftShoulder = true,
                RightShoulder = true,
                Back = true,
                Start = true,
                Guide = true,
                LeftThumb = true,
                RightThumb = true,
                DPadUp = true,
                DPadDown = true,
                DPadLeft = true,
                DPadRight = true,
                LeftTrigger = 0.85,
                RightTrigger = 0.95,
                LeftThumbX = -0.707,
                LeftThumbY = 0.707,
                RightThumbX = 0.5,
                RightThumbY = -0.5
            };

            state.Reset();

            Assert.False(state.A);
            Assert.False(state.B);
            Assert.False(state.X);
            Assert.False(state.Y);
            Assert.False(state.LeftShoulder);
            Assert.False(state.RightShoulder);
            Assert.False(state.Back);
            Assert.False(state.Start);
            Assert.False(state.Guide);
            Assert.False(state.LeftThumb);
            Assert.False(state.RightThumb);
            Assert.False(state.DPadUp);
            Assert.False(state.DPadDown);
            Assert.False(state.DPadLeft);
            Assert.False(state.DPadRight);

            Assert.Equal(0.0, state.LeftTrigger);
            Assert.Equal(0.0, state.RightTrigger);
            Assert.Equal(0.0, state.LeftThumbX);
            Assert.Equal(0.0, state.LeftThumbY);
            Assert.Equal(0.0, state.RightThumbX);
            Assert.Equal(0.0, state.RightThumbY);
        }

        [Fact]
        public void MockController_Disconnect_ResetsState()
        {
            var mock = new MockVirtualController();
            mock.Connect();

            var activeState = new ControllerState
            {
                A = true,
                LeftTrigger = 1.0,
                LeftThumbX = 1.0
            };

            mock.UpdateState(activeState);
            Assert.True(mock.LastState.A);
            Assert.Equal(1.0, mock.LastState.LeftTrigger);

            mock.Disconnect();
            Assert.False(mock.IsConnected);
            Assert.False(mock.LastState.A);
            Assert.Equal(0.0, mock.LastState.LeftTrigger);
            Assert.Equal(0.0, mock.LastState.LeftThumbX);
        }
    }
}
