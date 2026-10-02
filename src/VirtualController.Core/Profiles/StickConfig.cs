using System;
using System.Text.Json.Serialization;

namespace VirtualController.Profiles
{
    /// <summary>
    /// Configures input source (Keyboard or Mouse) and parameters for a thumbstick.
    /// Can be applied to either Left Stick or Right Stick.
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
        public string ResponseCurve { get; set; } = "Linear";

        // --- Shared / General Parameters ---
        public double Deadzone { get; set; } = 0.10;
        public double Sensitivity { get; set; } = 1.0;

        // --- Mouse Settings ---
        public double Smoothing { get; set; } = 0.25;
        public double MaxMagnitude { get; set; } = 1.0;
        public bool InvertX { get; set; } = false;
        public bool InvertY { get; set; } = false;
        public double Acceleration { get; set; } = 1.0;
        public double DecayRate { get; set; } = 0.80;

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
                Deadzone = 0.05,
                Smoothing = 0.25,
                MaxMagnitude = 1.0,
                InvertX = false,
                InvertY = false,
                Acceleration = 1.0,
                DecayRate = 0.80
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
                Smoothing = this.Smoothing,
                MaxMagnitude = this.MaxMagnitude,
                InvertX = this.InvertX,
                InvertY = this.InvertY,
                Acceleration = this.Acceleration,
                DecayRate = this.DecayRate
            };
        }
    }
}
