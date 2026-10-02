using System;
using VirtualController.Profiles;

namespace VirtualController.Core
{
    /// <summary>
    /// Converts raw physical InputState into normalized ControllerState
    /// according to the active Profile rules (analog normalization, deadzones, curves, mappings).
    /// Supports assigning Keyboard or Mouse to Left Stick, Right Stick, or both.
    /// </summary>
    public class MappingEngine
    {
        private double _leftMouseStickX = 0.0;
        private double _leftMouseStickY = 0.0;
        private double _rightMouseStickX = 0.0;
        private double _rightMouseStickY = 0.0;

        /// <summary>
        /// Transforms raw physical input into an Xbox 360 controller state.
        /// </summary>
        public ControllerState Process(InputState input, Profile profile, ControllerState? existingState = null)
        {
            var output = existingState ?? new ControllerState();
            output.Reset();

            if (input == null || profile == null)
            {
                Reset();
                return output;
            }

            var (mouseDx, mouseDy) = input.HarvestMouseDelta();

            // 1. Process Left Stick (Keyboard or Mouse)
            var (lx, ly) = ProcessStick(profile.LeftStick, input, mouseDx, mouseDy, ref _leftMouseStickX, ref _leftMouseStickY);
            output.LeftThumbX = lx;
            output.LeftThumbY = ly;

            // 2. Process Right Stick (Keyboard or Mouse)
            var (rx, ry) = ProcessStick(profile.RightStick, input, mouseDx, mouseDy, ref _rightMouseStickX, ref _rightMouseStickY);
            output.RightThumbX = rx;
            output.RightThumbY = ry;

            // 3. Process Buttons and Triggers
            ProcessButtons(input, profile, output);

            return output;
        }

        public static (double x, double y) CalculateKeyboardStick(
            bool up, bool down, bool left, bool right,
            double deadzone = 0.0, double sensitivity = 1.0, string curve = "Linear")
        {
            double rawX = 0.0;
            double rawY = 0.0;

            if (right) rawX += 1.0;
            if (left)  rawX -= 1.0;
            if (up)    rawY += 1.0;
            if (down)  rawY -= 1.0;

            if (rawX == 0.0 && rawY == 0.0)
            {
                return (0.0, 0.0);
            }

            double magnitude = Math.Sqrt(rawX * rawX + rawY * rawY);

            // Normalize diagonal magnitude so it never exceeds 1.0
            double dirX = rawX / magnitude;
            double dirY = rawY / magnitude;

            if (magnitude > 1.0)
            {
                magnitude = 1.0;
            }

            // Apply deadzone
            if (magnitude < deadzone)
            {
                return (0.0, 0.0);
            }

            // Scale magnitude above deadzone
            double scaledMag = (magnitude - deadzone) / (1.0 - deadzone);

            if (string.Equals(curve, "Exponential", StringComparison.OrdinalIgnoreCase))
            {
                scaledMag = scaledMag * scaledMag;
            }

            scaledMag *= sensitivity;
            if (scaledMag > 1.0) scaledMag = 1.0;

            double finalX = Math.Clamp(dirX * scaledMag, -1.0, 1.0);
            double finalY = Math.Clamp(dirY * scaledMag, -1.0, 1.0);

            return (finalX, finalY);
        }

        private (double x, double y) ProcessStick(
            StickConfig config,
            InputState input,
            int mouseDx,
            int mouseDy,
            ref double currentMouseX,
            ref double currentMouseY)
        {
            if (config.IsMouse)
            {
                return ProcessMouseStick(config, mouseDx, mouseDy, ref currentMouseX, ref currentMouseY);
            }
            else
            {
                bool up = input.IsPhysicalInputActive(config.Up);
                bool down = input.IsPhysicalInputActive(config.Down);
                bool left = input.IsPhysicalInputActive(config.Left);
                bool right = input.IsPhysicalInputActive(config.Right);

                return CalculateKeyboardStick(
                    up, down, left, right,
                    config.Deadzone, config.Sensitivity, config.ResponseCurve);
            }
        }

        private (double x, double y) ProcessMouseStick(
            StickConfig settings,
            int dx,
            int dy,
            ref double currentMouseX,
            ref double currentMouseY)
        {
            // Screen mouse Y is down-positive; gamepad Y is up-positive
            double mouseX = dx;
            double mouseY = -dy;

            if (settings.InvertX) mouseX = -mouseX;
            if (settings.InvertY) mouseY = -mouseY;

            double mouseMag = Math.Sqrt(mouseX * mouseX + mouseY * mouseY);

            if (mouseMag > 0.0001)
            {
                // Optional acceleration
                double accelMultiplier = 1.0;
                if (settings.Acceleration > 1.0)
                {
                    accelMultiplier = Math.Pow(mouseMag, settings.Acceleration - 1.0);
                }

                // Sensitivity factor (normalized against typical 10-50 delta range)
                double factor = 0.05 * settings.Sensitivity * accelMultiplier;
                double targetX = mouseX * factor;
                double targetY = mouseY * factor;

                double targetMag = Math.Sqrt(targetX * targetX + targetY * targetY);
                if (targetMag > settings.MaxMagnitude)
                {
                    targetX = (targetX / targetMag) * settings.MaxMagnitude;
                    targetY = (targetY / targetMag) * settings.MaxMagnitude;
                    targetMag = settings.MaxMagnitude;
                }

                if (targetMag < settings.Deadzone)
                {
                    targetX = 0;
                    targetY = 0;
                }

                // Apply smoothing
                double s = Math.Clamp(settings.Smoothing, 0.0, 0.95);
                currentMouseX = (currentMouseX * s) + (targetX * (1.0 - s));
                currentMouseY = (currentMouseY * s) + (targetY * (1.0 - s));
            }
            else
            {
                // Mouse is stationary: apply decay to smoothly return to center
                double decay = Math.Clamp(settings.DecayRate, 0.0, 1.0);
                currentMouseX *= decay;
                currentMouseY *= decay;

                if (Math.Abs(currentMouseX) < 0.01) currentMouseX = 0.0;
                if (Math.Abs(currentMouseY) < 0.01) currentMouseY = 0.0;
            }

            currentMouseX = Math.Clamp(currentMouseX, -1.0, 1.0);
            currentMouseY = Math.Clamp(currentMouseY, -1.0, 1.0);

            return (currentMouseX, currentMouseY);
        }

        private void ProcessButtons(InputState input, Profile profile, ControllerState output)
        {
            // Primary button mapping: Virtual Button Name -> Physical Input Name
            foreach (var kvp in profile.Buttons)
            {
                string vButton = kvp.Key;
                string pInput = kvp.Value;

                if (input.IsPhysicalInputActive(pInput))
                {
                    ApplyVirtualButton(vButton, true, output);
                }
            }

            // Extra mappings: Physical Input Name -> Virtual Button Name
            foreach (var kvp in profile.ExtraMappings)
            {
                string pInput = kvp.Key;
                string vButton = kvp.Value;

                if (input.IsPhysicalInputActive(pInput))
                {
                    ApplyVirtualButton(vButton, true, output);
                }
            }
        }

        private void ApplyVirtualButton(string vButton, bool active, ControllerState output)
        {
            switch (vButton.ToUpperInvariant())
            {
                case "A": output.A = active; break;
                case "B": output.B = active; break;
                case "X": output.X = active; break;
                case "Y": output.Y = active; break;
                case "LB": output.LeftShoulder = active; break;
                case "RB": output.RightShoulder = active; break;
                case "LT": output.LeftTrigger = active ? 1.0 : 0.0; break;
                case "RT": output.RightTrigger = active ? 1.0 : 0.0; break;
                case "BACK": output.Back = active; break;
                case "START": output.Start = active; break;
                case "GUIDE": output.Guide = active; break;
                case "LS":
                case "LEFTTHUMB": output.LeftThumb = active; break;
                case "RS":
                case "RIGHTTHUMB": output.RightThumb = active; break;
                case "DPADUP": output.DPadUp = active; break;
                case "DPADDOWN": output.DPadDown = active; break;
                case "DPADLEFT": output.DPadLeft = active; break;
                case "DPADRIGHT": output.DPadRight = active; break;
            }
        }

        public void Reset()
        {
            _leftMouseStickX = 0.0;
            _leftMouseStickY = 0.0;
            _rightMouseStickX = 0.0;
            _rightMouseStickY = 0.0;
        }
    }
}
