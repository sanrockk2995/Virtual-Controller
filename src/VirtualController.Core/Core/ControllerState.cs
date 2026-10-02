using System;

namespace VirtualController.Core
{
    /// <summary>
    /// Represents the normalized state of an Xbox 360 controller.
    /// Buttons, triggers (0.0 to 1.0), and analog thumbsticks (-1.0 to +1.0).
    /// </summary>
    public class ControllerState
    {
        // Standard Digital Buttons
        public bool A { get; set; }
        public bool B { get; set; }
        public bool X { get; set; }
        public bool Y { get; set; }

        public bool LeftShoulder { get; set; }  // LB
        public bool RightShoulder { get; set; } // RB

        public bool Back { get; set; }
        public bool Start { get; set; }
        public bool Guide { get; set; }

        public bool LeftThumb { get; set; }  // LS Click
        public bool RightThumb { get; set; } // RS Click

        // D-Pad
        public bool DPadUp { get; set; }
        public bool DPadDown { get; set; }
        public bool DPadLeft { get; set; }
        public bool DPadRight { get; set; }

        // Analog Triggers (0.0 to 1.0)
        public double LeftTrigger { get; set; }
        public double RightTrigger { get; set; }

        // Analog Sticks (-1.0 to +1.0)
        public double LeftThumbX { get; set; }
        public double LeftThumbY { get; set; }

        public double RightThumbX { get; set; }
        public double RightThumbY { get; set; }

        /// <summary>
        /// Resets all inputs to neutral/released state.
        /// </summary>
        public void Reset()
        {
            A = false;
            B = false;
            X = false;
            Y = false;

            LeftShoulder = false;
            RightShoulder = false;

            Back = false;
            Start = false;
            Guide = false;

            LeftThumb = false;
            RightThumb = false;

            DPadUp = false;
            DPadDown = false;
            DPadLeft = false;
            DPadRight = false;

            LeftTrigger = 0.0;
            RightTrigger = 0.0;

            LeftThumbX = 0.0;
            LeftThumbY = 0.0;

            RightThumbX = 0.0;
            RightThumbY = 0.0;
        }

        /// <summary>
        /// Returns a deep copy of this ControllerState.
        /// </summary>
        public ControllerState Clone()
        {
            return new ControllerState
            {
                A = this.A,
                B = this.B,
                X = this.X,
                Y = this.Y,
                LeftShoulder = this.LeftShoulder,
                RightShoulder = this.RightShoulder,
                Back = this.Back,
                Start = this.Start,
                Guide = this.Guide,
                LeftThumb = this.LeftThumb,
                RightThumb = this.RightThumb,
                DPadUp = this.DPadUp,
                DPadDown = this.DPadDown,
                DPadLeft = this.DPadLeft,
                DPadRight = this.DPadRight,
                LeftTrigger = this.LeftTrigger,
                RightTrigger = this.RightTrigger,
                LeftThumbX = this.LeftThumbX,
                LeftThumbY = this.LeftThumbY,
                RightThumbX = this.RightThumbX,
                RightThumbY = this.RightThumbY
            };
        }
    }
}
