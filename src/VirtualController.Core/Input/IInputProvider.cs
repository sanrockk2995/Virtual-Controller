using System;
using VirtualController.Core;

namespace VirtualController.Input
{
    public interface IInputProvider : IDisposable
    {
        bool IsRunning { get; }
        void Start();
        void Stop();
        event EventHandler<InputState>? InputUpdated;
    }
}
