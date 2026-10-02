using System;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Exceptions;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using VirtualController.Core;
using VirtualController.Services;

namespace VirtualController.Backend
{
    public class ViGEmController : IVirtualController
    {
        private ViGEmClient? _client;
        private IXbox360Controller? _controller;
        private readonly object _syncLock = new();
        private bool _disposed;

        public bool IsConnected { get; private set; }
        public string DriverStatusMessage { get; private set; } = "Not initialized";

        public event EventHandler<bool>? ConnectionChanged;

        public ViGEmController()
        {
            InitializeClient();
        }

        private void InitializeClient()
        {
            try
            {
                _client = new ViGEmClient();
                DriverStatusMessage = "ViGEmBus client connected successfully.";
                AppLogger.Info("ViGEmBus client initialized successfully.");
            }
            catch (VigemBusNotFoundException ex)
            {
                DriverStatusMessage = "ViGEmBus driver not found. Please install ViGEmBus driver.";
                AppLogger.Error("ViGEmBus driver not found.", ex);
            }
            catch (VigemBusAccessFailedException ex)
            {
                DriverStatusMessage = "Access to ViGEmBus failed. Check administrative permissions.";
                AppLogger.Error("Access to ViGEmBus failed.", ex);
            }
            catch (Exception ex)
            {
                DriverStatusMessage = $"ViGEm initialization error: {ex.Message}";
                AppLogger.Error("Unexpected ViGEm error.", ex);
            }
        }

        public bool Reinitialize()
        {
            lock (_syncLock)
            {
                if (IsConnected) return true;

                try
                {
                    _controller?.Disconnect();
                    _controller = null;
                    _client?.Dispose();
                    _client = null;
                }
                catch { }

                InitializeClient();
                return _client != null;
            }
        }

        public bool Connect()
        {
            lock (_syncLock)
            {
                if (IsConnected) return true;

                if (_client == null)
                {
                    InitializeClient();
                    if (_client == null)
                    {
                        return false;
                    }
                }

                try
                {
                    _controller = _client.CreateXbox360Controller();
                    _controller.Connect();
                    IsConnected = true;
                    DriverStatusMessage = "Virtual Xbox 360 controller connected.";
                    AppLogger.Info("Virtual Xbox 360 controller plugged in.");
                    ConnectionChanged?.Invoke(this, true);
                    return true;
                }
                catch (Exception ex)
                {
                    DriverStatusMessage = $"Connection failed: {ex.Message}";
                    AppLogger.Error("Failed to connect virtual Xbox 360 controller.", ex);
                    IsConnected = false;
                    ConnectionChanged?.Invoke(this, false);
                    return false;
                }
            }
        }

        public void Disconnect()
        {
            lock (_syncLock)
            {
                if (!IsConnected || _controller == null) return;

                try
                {
                    Reset();
                    _controller.Disconnect();
                    AppLogger.Info("Virtual Xbox 360 controller disconnected.");
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Error during controller disconnect.", ex);
                }
                finally
                {
                    _controller = null;
                    IsConnected = false;
                    DriverStatusMessage = "Virtual controller disconnected.";
                    ConnectionChanged?.Invoke(this, false);
                }
            }
        }

        public void UpdateState(ControllerState state)
        {
            lock (_syncLock)
            {
                if (!IsConnected || _controller == null || _disposed) return;

                try
                {
                    // Buttons
                    _controller.SetButtonState(Xbox360Button.A, state.A);
                    _controller.SetButtonState(Xbox360Button.B, state.B);
                    _controller.SetButtonState(Xbox360Button.X, state.X);
                    _controller.SetButtonState(Xbox360Button.Y, state.Y);

                    _controller.SetButtonState(Xbox360Button.LeftShoulder, state.LeftShoulder);
                    _controller.SetButtonState(Xbox360Button.RightShoulder, state.RightShoulder);

                    _controller.SetButtonState(Xbox360Button.Back, state.Back);
                    _controller.SetButtonState(Xbox360Button.Start, state.Start);
                    _controller.SetButtonState(Xbox360Button.Guide, state.Guide);

                    _controller.SetButtonState(Xbox360Button.LeftThumb, state.LeftThumb);
                    _controller.SetButtonState(Xbox360Button.RightThumb, state.RightThumb);

                    _controller.SetButtonState(Xbox360Button.Up, state.DPadUp);
                    _controller.SetButtonState(Xbox360Button.Down, state.DPadDown);
                    _controller.SetButtonState(Xbox360Button.Left, state.DPadLeft);
                    _controller.SetButtonState(Xbox360Button.Right, state.DPadRight);

                    // Triggers (0.0 to 1.0 -> 0 to 255)
                    byte lt = (byte)Math.Clamp((int)(state.LeftTrigger * 255.0), 0, 255);
                    byte rt = (byte)Math.Clamp((int)(state.RightTrigger * 255.0), 0, 255);
                    _controller.SetSliderValue(Xbox360Slider.LeftTrigger, lt);
                    _controller.SetSliderValue(Xbox360Slider.RightTrigger, rt);

                    // Analog Sticks (-1.0 to 1.0 -> -32768 to 32767)
                    short lx = (short)Math.Clamp((int)(state.LeftThumbX * 32767.0), -32768, 32767);
                    short ly = (short)Math.Clamp((int)(state.LeftThumbY * 32767.0), -32768, 32767);
                    short rx = (short)Math.Clamp((int)(state.RightThumbX * 32767.0), -32768, 32767);
                    short ry = (short)Math.Clamp((int)(state.RightThumbY * 32767.0), -32768, 32767);

                    _controller.SetAxisValue(Xbox360Axis.LeftThumbX, lx);
                    _controller.SetAxisValue(Xbox360Axis.LeftThumbY, ly);
                    _controller.SetAxisValue(Xbox360Axis.RightThumbX, rx);
                    _controller.SetAxisValue(Xbox360Axis.RightThumbY, ry);

                    // Submit report to driver in single atomic update
                    _controller.SubmitReport();
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Error updating virtual controller report.", ex);
                }
            }
        }

        public void Reset()
        {
            lock (_syncLock)
            {
                if (_controller == null || !IsConnected) return;

                try
                {
                    // Clear all buttons
                    _controller.SetButtonState(Xbox360Button.A, false);
                    _controller.SetButtonState(Xbox360Button.B, false);
                    _controller.SetButtonState(Xbox360Button.X, false);
                    _controller.SetButtonState(Xbox360Button.Y, false);
                    _controller.SetButtonState(Xbox360Button.LeftShoulder, false);
                    _controller.SetButtonState(Xbox360Button.RightShoulder, false);
                    _controller.SetButtonState(Xbox360Button.Back, false);
                    _controller.SetButtonState(Xbox360Button.Start, false);
                    _controller.SetButtonState(Xbox360Button.Guide, false);
                    _controller.SetButtonState(Xbox360Button.LeftThumb, false);
                    _controller.SetButtonState(Xbox360Button.RightThumb, false);
                    _controller.SetButtonState(Xbox360Button.Up, false);
                    _controller.SetButtonState(Xbox360Button.Down, false);
                    _controller.SetButtonState(Xbox360Button.Left, false);
                    _controller.SetButtonState(Xbox360Button.Right, false);

                    // Neutralize triggers
                    _controller.SetSliderValue(Xbox360Slider.LeftTrigger, 0);
                    _controller.SetSliderValue(Xbox360Slider.RightTrigger, 0);

                    // Neutralize sticks
                    _controller.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
                    _controller.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
                    _controller.SetAxisValue(Xbox360Axis.RightThumbX, 0);
                    _controller.SetAxisValue(Xbox360Axis.RightThumbY, 0);

                    _controller.SubmitReport();
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Error resetting controller state.", ex);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Disconnect();

            lock (_syncLock)
            {
                try
                {
                    _client?.Dispose();
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Error disposing ViGEmClient.", ex);
                }
                _client = null;
            }
        }
    }
}
