using System;
using System.Collections.Generic;

namespace VirtualController.Profiles
{
    public class Profile
    {
        public string Name { get; set; } = "Default Keyboard + Mouse";

        public StickConfig LeftStick { get; set; } = StickConfig.CreateDefaultLeft();

        public StickConfig RightStick { get; set; } = StickConfig.CreateDefaultRight();

        /// <summary>
        /// Maps Virtual Controller Button/Trigger -> Physical Input name.
        /// Keys: A, B, X, Y, LB, RB, LT, RT, Back, Start, LS, RS, DPadUp, DPadDown, DPadLeft, DPadRight
        /// Values: Key names ("Space", "LeftShift", "W") or Mouse buttons ("LeftMouse", "RightMouse", "MiddleMouse")
        /// </summary>
        public Dictionary<string, string> Buttons { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Additional physical inputs that map to a virtual button.
        /// For instance, Escape -> Start (in addition to Enter -> Start).
        /// Key: Physical Input, Value: Virtual Controller Button.
        /// </summary>
        public Dictionary<string, string> ExtraMappings { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public static Profile CreateDefaultProfile()
        {
            var p = new Profile
            {
                Name = "Default Keyboard + Mouse",
                LeftStick = StickConfig.CreateDefaultLeft(),
                RightStick = StickConfig.CreateDefaultRight(),
                Buttons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["A"] = "Space",
                    ["B"] = "LeftShift",
                    ["X"] = "E",
                    ["Y"] = "Q",
                    ["LB"] = "F",
                    ["RB"] = "R",
                    ["LT"] = "RightMouse",
                    ["RT"] = "LeftMouse",
                    ["Back"] = "Tab",
                    ["Start"] = "Enter",
                    ["RS"] = "MiddleMouse",
                    ["LS"] = "C"
                },
                ExtraMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Escape"] = "Start"
                }
            };

            return p;
        }

        public Profile Clone()
        {
            return new Profile
            {
                Name = this.Name + " (Copy)",
                LeftStick = this.LeftStick.Clone(),
                RightStick = this.RightStick.Clone(),
                Buttons = new Dictionary<string, string>(this.Buttons, StringComparer.OrdinalIgnoreCase),
                ExtraMappings = new Dictionary<string, string>(this.ExtraMappings, StringComparer.OrdinalIgnoreCase)
            };
        }
    }
}
