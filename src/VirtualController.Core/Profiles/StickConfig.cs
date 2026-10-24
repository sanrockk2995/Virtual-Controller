using System;
using System.Text.Json.Serialization;

namespace VirtualController.Profiles
{
    /// <summary>
    /// Configures input source (Keyboard or Mouse) and parameters for a thumbstick.
    /// Can be applied to either Left Stick or Right Stick.
    /// Supports advanced mouse-to-gamepad translation: Anti-Deadzone, independent X/Y sensitivity,
    /// response curves, smoothing, and radial/squared stick limits.
    /// </summary>
    public class StickConfig
    {
        private string _inputSource = "Keyboard";

        /// <summary>
        /// Input source: "Keyboard" or "Mouse"
        /// </summary>
        public string InputSource
        {
            get => _inputSource;
            set => _inputSource = string.Equals(value, "Mouse", StringComparison.OrdinalIgnoreCase) ? "Mouse" : "Keyboard";
        }

        [JsonIgnore]
        public bool IsMouse => string.Equals(InputSource, "Mouse", StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public bool IsKeyboard => !IsMouse;

        // --- Keyboard Settings ---
        public string Up { get; set; } = "W";
        public string Down { get; set; } = "S";
        public string Left { get; set; } = "A";
        public string Right { get; set; } = "D";

        // --- Shared / General Parameters ---
        public string ResponseCurve { get; set; } = "Linear";
        public double Deadzone { get; set; } = 0.05;
        public double Sensitivity { get; set; } = 1.0;

        // --- Advanced Mouse-to-Gamepad Translation Parameters ---
        public double SensitivityX { get; set; } = 1.0;
        public double SensitivityY { get; set; } = 1.0;
        public double AntiDeadzone { get; set; } = 0.15;
        public double InitialSpeed { get; set; } = 0.5;

        public double Smoothing { get; set; } = 0.15;
        public double MaxMagnitude { get; set; } = 1.0;

        [JsonIgnore]
        public double MaxDeflection
        {
            get => MaxMagnitude;
            set => MaxMagnitude = value;
        }

        public bool InvertX { get; set; } = false;
        public bool InvertY { get; set; } = false;
        public double Acceleration { get; set; } = 1.0;
        public double DecayRate { get; set; } = 0.0;
        public bool IsRadial { get; set; } = true;

        [JsonIgnore]
        public string ReturnToCenterMode
        {
            get => DecayRate <= 0.01 ? "Instant" : "Decay";
            set => DecayRate = string.Equals(value, "Instant", StringComparison.OrdinalIgnoreCase) ? 0.0 : (DecayRate > 0.01 ? DecayRate : 0.50);
        }

        public static StickConfig CreateDefaultLeft()
        {
            return new StickConfig
            {
                InputSource = "Keyboard",
                Up = "W",
                Down = "S",
                Left = "A",
                Right = "D",
                Deadzone = 0.10,
                Sensitivity = 1.0,
                ResponseCurve = "Linear"
            };
        }

        public static StickConfig CreateDefaultRight()
        {
            return new StickConfig
            {
                InputSource = "Mouse",
                Up = "Up",
                Down = "Down",
                Left = "Left",
                Right = "Right",
                Sensitivity = 1.2,
                SensitivityX = 1.0,
                SensitivityY = 1.0,
                AntiDeadzone = 0.15,
                InitialSpeed = 0.5,
                Deadzone = 0.05,
                Smoothing = 0.15,
                MaxMagnitude = 1.0,
                InvertX = false,
                InvertY = false,
                Acceleration = 1.0,
                DecayRate = 0.0,
                IsRadial = true,
                ResponseCurve = "Linear"
            };
        }

        public StickConfig Clone()
        {
            return new StickConfig
            {
                InputSource = this.InputSource,
                Up = this.Up,
                Down = this.Down,
                Left = this.Left,
                Right = this.Right,
                ResponseCurve = this.ResponseCurve,
                Deadzone = this.Deadzone,
                Sensitivity = this.Sensitivity,
                SensitivityX = this.SensitivityX,
                SensitivityY = this.SensitivityY,
                AntiDeadzone = this.AntiDeadzone,
                InitialSpeed = this.InitialSpeed,
                Smoothing = this.Smoothing,
                MaxMagnitude = this.MaxMagnitude,
                InvertX = this.InvertX,
                InvertY = this.InvertY,
                Acceleration = this.Acceleration,
                DecayRate = this.DecayRate,
                IsRadial = this.IsRadial
            };
        }
    }
}
