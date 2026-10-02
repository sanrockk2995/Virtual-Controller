using System;
using System.Collections.Generic;

namespace VirtualController.Input
{
    public static class KeyNameHelper
    {
        private static readonly Dictionary<int, string> VkToStringMap = new()
        {
            [0x08] = "Backspace",
            [0x09] = "Tab",
            [0x0D] = "Enter",
            [0x10] = "Shift",
            [0x11] = "Ctrl",
            [0x12] = "Alt",
            [0x13] = "Pause",
            [0x14] = "CapsLock",
            [0x1B] = "Escape",
            [0x20] = "Space",
            [0x21] = "PageUp",
            [0x22] = "PageDown",
            [0x23] = "End",
            [0x24] = "Home",
            [0x25] = "Left",
            [0x26] = "Up",
            [0x27] = "Right",
            [0x28] = "Down",
            [0x2C] = "PrintScreen",
            [0x2D] = "Insert",
            [0x2E] = "Delete",
            [0xA0] = "LeftShift",
            [0xA1] = "RightShift",
            [0xA2] = "LeftCtrl",
            [0xA3] = "RightCtrl",
            [0xA4] = "LeftAlt",
            [0xA5] = "RightAlt",
            [0x70] = "F1",
            [0x71] = "F2",
            [0x72] = "F3",
            [0x73] = "F4",
            [0x74] = "F5",
            [0x75] = "F6",
            [0x76] = "F7",
            [0x77] = "F8",
            [0x78] = "F9",
            [0x79] = "F10",
            [0x7A] = "F11",
            [0x7B] = "F12"
        };

        public static string GetKeyName(int vkCode)
        {
            if (vkCode >= 0x41 && vkCode <= 0x5A) // A-Z
            {
                return ((char)vkCode).ToString();
            }

            if (vkCode >= 0x30 && vkCode <= 0x39) // 0-9
            {
                return ((char)vkCode).ToString();
            }

            if (VkToStringMap.TryGetValue(vkCode, out var name))
            {
                return name;
            }

            return $"Key_0x{vkCode:X2}";
        }

        public static int? GetVkCode(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            if (name.Length == 1)
            {
                char c = char.ToUpperInvariant(name[0]);
                if (c >= 'A' && c <= 'Z') return (int)c;
                if (c >= '0' && c <= '9') return (int)c;
            }

            foreach (var kvp in VkToStringMap)
            {
                if (kvp.Value.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Key;
                }
            }

            if (name.Equals("Shift", StringComparison.OrdinalIgnoreCase)) return 0x10;
            if (name.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)) return 0x11;
            if (name.Equals("Alt", StringComparison.OrdinalIgnoreCase)) return 0x12;

            return null;
        }
    }
}
