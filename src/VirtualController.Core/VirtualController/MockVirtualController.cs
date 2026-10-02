using System;
using VirtualController.Core;

namespace VirtualController.Backend
{
    public class MockVirtualController : IVirtualController
    {
        public bool IsConnected { get; private set; }
        public string DriverStatusMessage { get; set; } = "Mock controller ready.";
        public ControllerState LastState { get; } = new();

        public event EventHandler<bool>? ConnectionChanged;

        public bool Connect()
        {
            IsConnected = true;
            DriverStatusMessage = "Mock controller connected.";
            ConnectionChanged?.Invoke(this, true);
            return true;
        }

        public void Disconnect()
        {
            Reset();
            IsConnected = false;
            DriverStatusMessage = "Mock controller disconnected.";
            ConnectionChanged?.Invoke(this, false);
        }

        public bool Reinitialize()
        {
            return true;
        }

        public void UpdateState(ControllerState state)
        {
            if (!IsConnected) return;

            LastState.A = state.A;
            LastState.B = state.B;
            LastState.X = state.X;
            LastState.Y = state.Y;
            LastState.LeftShoulder = state.LeftShoulder;
            LastState.RightShoulder = state.RightShoulder;
            LastState.Back = state.Back;
            LastState.Start = state.Start;
            LastState.Guide = state.Guide;
            LastState.LeftThumb = state.LeftThumb;
            LastState.RightThumb = state.RightThumb;
            LastState.DPadUp = state.DPadUp;
            LastState.DPadDown = state.DPadDown;
            LastState.DPadLeft = state.DPadLeft;
            LastState.DPadRight = state.DPadRight;
            LastState.LeftTrigger = state.LeftTrigger;
            LastState.RightTrigger = state.RightTrigger;
            LastState.LeftThumbX = state.LeftThumbX;
            LastState.LeftThumbY = state.LeftThumbY;
            LastState.RightThumbX = state.RightThumbX;
            LastState.RightThumbY = state.RightThumbY;
        }

        public void Reset()
        {
            LastState.Reset();
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
