using System;
using System.Threading;
using VirtualController.Core;
using VirtualController.Profiles;
using VirtualController.Services;
using VirtualController.Backend;

namespace VirtualController.Input
{
    public class InputManager : IDisposable
    {
        private readonly IVirtualController _controller;
        private readonly ProfileManager _profileManager;
        private readonly MappingEngine _mappingEngine = new();
        private readonly InputState _inputState = new();
        private readonly WindowsHookManager _hookManager;
        private readonly RawInputReceiver _rawInputReceiver;

        private readonly ControllerState _liveState = new();
        private Thread? _workerThread;
        private readonly CancellationTokenSource _cts = new();
        private readonly object _stateLock = new();

        public bool IsEnabled { get; private set; }
        public bool IsActive => IsEnabled && _controller.IsConnected;

        public event EventHandler<ControllerState>? StateUpdated;
        public event EventHandler<bool>? EnabledChanged;

        public InputManager(IVirtualController controller, ProfileManager profileManager)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));

            _hookManager = new WindowsHookManager(_inputState);
            _hookManager.EmergencyDisableRequested += OnEmergencyDisable;
            _hookManager.ToggleRequested += OnToggleRequested;

            _profileManager.ActiveProfileChanged += OnActiveProfileChanged;

            _rawInputReceiver = new RawInputReceiver(_inputState);

            StartProcessingThread();
        }

        private void StartProcessingThread()
        {
            _workerThread = new Thread(ProcessLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
                Name = "InputProcessingLoop"
            };
            _workerThread.Start();
        }

        private void ProcessLoop()
        {
            var token = _cts.Token;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (IsEnabled && _controller.IsConnected)
                    {
                        var profile = _profileManager.ActiveProfile;

                        lock (_stateLock)
                        {
                            _mappingEngine.Process(_inputState, profile, _liveState);
                            _controller.UpdateState(_liveState);
                        }

                        // Notify listeners (UI Tester) with snapshot copy
                        StateUpdated?.Invoke(this, _liveState.Clone());
                    }
                    else
                    {
                        // When disabled, clear mouse accumulator and live state
                        _inputState.HarvestMouseDelta();
                        lock (_stateLock)
                        {
                            _liveState.Reset();
                        }
                    }

                    // Run at ~125 Hz (8ms) for low latency input response
                    Thread.Sleep(8);
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Exception in input processing loop.", ex);
                    Thread.Sleep(20);
                }
            }
        }

        public bool Enable()
        {
            if (IsEnabled) return true;

            try
            {
                if (!_controller.IsConnected)
                {
                    bool connected = _controller.Connect();
                    if (!connected)
                    {
                        AppLogger.Warning("Cannot enable virtual controller: Driver failed to connect.");
                        return false;
                    }
                }

                _hookManager.Start();
                _rawInputReceiver.Start();
                IsEnabled = true;
                AppLogger.Info("Virtual controller input processing enabled.");
                EnabledChanged?.Invoke(this, true);
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Failed to enable virtual controller input.", ex);
                return false;
            }
        }

        public void Disable()
        {
            if (!IsEnabled) return;

            try
            {
                _rawInputReceiver.Stop();
                _hookManager.Stop();
                _inputState.Clear();
                _mappingEngine.Reset();

                lock (_stateLock)
                {
                    _liveState.Reset();
                }

                _controller.Reset();
                _controller.Disconnect();

                IsEnabled = false;
                AppLogger.Info("Virtual controller input processing disabled.");
                EnabledChanged?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Error disabling virtual controller.", ex);
            }
        }

        private void OnActiveProfileChanged(object? sender, Profile profile)
        {
            lock (_stateLock)
            {
                _mappingEngine.Reset();
                _inputState.HarvestMouseDelta();
                _liveState.Reset();
            }
        }

        public void EmergencyDisable()
        {
            AppLogger.Warning("EMERGENCY DISABLE triggered!");
            Disable();
        }

        private void OnEmergencyDisable()
        {
            EmergencyDisable();
        }

        private void OnToggleRequested()
        {
            // If already disabled by emergency handler, do nothing; else toggle
            // Handled safely
        }

        public void Toggle()
        {
            if (IsEnabled)
            {
                Disable();
            }
            else
            {
                Enable();
            }
        }

        public ControllerState GetCurrentStateSnapshot()
        {
            lock (_stateLock)
            {
                return _liveState.Clone();
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            Disable();

            _profileManager.ActiveProfileChanged -= OnActiveProfileChanged;
            _rawInputReceiver.Dispose();
            _hookManager.Dispose();
            _controller.Dispose();

            try
            {
                _workerThread?.Join(1000);
            }
            catch
            {
                // Ignored
            }
        }
    }
}
