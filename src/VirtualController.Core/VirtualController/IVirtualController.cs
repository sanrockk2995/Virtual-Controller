using System;
using VirtualController.Core;

namespace VirtualController.Backend
{
    public interface IVirtualController : IDisposable
    {
        bool IsConnected { get; }
        string DriverStatusMessage { get; }

        event EventHandler<bool>? ConnectionChanged;

        bool Connect();
        void Disconnect();
        bool Reinitialize();
        void UpdateState(ControllerState state);
        void Reset();
    }
}
