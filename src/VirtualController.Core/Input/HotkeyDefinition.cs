using System;
using System.Collections.Generic;

namespace VirtualController.Input
{
    /// <summary>
    /// Represents a keyboard shortcut combination (e.g. Ctrl + Alt + F10, Ctrl + Shift + F12, F11).
    /// </summary>
    public class HotkeyDefinition
    {
        public bool RequireCtrl { get; set; } = true;
        public bool RequireAlt { get; set; } = true;
        public bool RequireShift { get; set; } = false;
        public int KeyVk { get; set; } = 0x79; // VK_F10
        public string KeyName { get; set; } = "F10";

        public HotkeyDefinition() { }

        public HotkeyDefinition(bool ctrl, bool alt, bool shift, int vk, string name)
        {
            RequireCtrl = ctrl;
            RequireAlt = alt;
            RequireShift = shift;
            KeyVk = vk;
            KeyName = name;
        }

        /// <summary>
        /// Checks whether the given event and physical modifier state match this hotkey definition.
        /// </summary>
        public bool Matches(int vkCode, Func<int, bool> isKeyDown)
        {
            if (vkCode != KeyVk) return false;

            bool ctrl = isKeyDown(0x11);  // VK_CONTROL
            bool alt = isKeyDown(0x12);   // VK_MENU
            bool shift = isKeyDown(0x10); // VK_SHIFT

            return (RequireCtrl == ctrl) && (RequireAlt == alt) && (RequireShift == shift);
        }

        public override string ToString()
        {
            var parts = new List<string>();
            if (RequireCtrl) parts.Add("Ctrl");
            if (RequireAlt) parts.Add("Alt");
            if (RequireShift) parts.Add("Shift");
            parts.Add(string.IsNullOrWhiteSpace(KeyName) ? KeyNameHelper.GetKeyName(KeyVk) : KeyName);
            return string.Join(" + ", parts);
        }

        public static HotkeyDefinition Parse(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new HotkeyDefinition(true, true, false, 0x79, "F10");
            }

            var parts = text.Split(new[] { '+', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            bool ctrl = false;
            bool alt = false;
            bool shift = false;
            string mainKey = "F10";

            foreach (var p in parts)
            {
                string s = p.Trim();
                if (s.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || s.Equals("Control", StringComparison.OrdinalIgnoreCase))
                    ctrl = true;
                else if (s.Equals("Alt", StringComparison.OrdinalIgnoreCase) || s.Equals("Menu", StringComparison.OrdinalIgnoreCase))
                    alt = true;
                else if (s.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                    shift = true;
                else
                    mainKey = s;
            }

            int? vk = KeyNameHelper.GetVkCode(mainKey);
            int keyVk = vk ?? 0x79;
            return new HotkeyDefinition(ctrl, alt, shift, keyVk, mainKey);
        }
    }
}
