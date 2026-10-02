using System;
using System.Collections.Generic;

namespace VirtualController.Core
{
    /// <summary>
    /// Represents the current raw physical state of Keyboard and Mouse.
    /// </summary>
    public class InputState
    {
        private readonly HashSet<string> _pressedKeys = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _pressedMouseButtons = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new();

        /// <summary>
        /// Accumulated relative mouse movement along X axis since last harvest.
        /// </summary>
        public int MouseDeltaX { get; set; }

        /// <summary>
        /// Accumulated relative mouse movement along Y axis since last harvest.
        /// </summary>
        public int MouseDeltaY { get; set; }

        public void SetKey(string keyName, bool isPressed)
        {
            if (string.IsNullOrWhiteSpace(keyName)) return;
            lock (_lock)
            {
                if (isPressed)
                    _pressedKeys.Add(keyName);
                else
                    _pressedKeys.Remove(keyName);
            }
        }

        public bool IsKeyPressed(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName)) return false;
            lock (_lock)
            {
                return _pressedKeys.Contains(keyName);
            }
        }

        public void SetMouseButton(string buttonName, bool isPressed)
        {
            if (string.IsNullOrWhiteSpace(buttonName)) return;
            lock (_lock)
            {
                if (isPressed)
                    _pressedMouseButtons.Add(buttonName);
                else
                    _pressedMouseButtons.Remove(buttonName);
            }
        }

        public bool IsMouseButtonPressed(string buttonName)
        {
            if (string.IsNullOrWhiteSpace(buttonName)) return false;
            lock (_lock)
            {
                return _pressedMouseButtons.Contains(buttonName);
            }
        }

        public void AddMouseDelta(int dx, int dy)
        {
            lock (_lock)
            {
                MouseDeltaX += dx;
                MouseDeltaY += dy;
            }
        }

        /// <summary>
        /// Returns and resets accumulated mouse delta.
        /// </summary>
        public (int dx, int dy) HarvestMouseDelta()
        {
            lock (_lock)
            {
                int dx = MouseDeltaX;
                int dy = MouseDeltaY;
                MouseDeltaX = 0;
                MouseDeltaY = 0;
                return (dx, dy);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _pressedKeys.Clear();
                _pressedMouseButtons.Clear();
                MouseDeltaX = 0;
                MouseDeltaY = 0;
            }
        }

        public bool IsPhysicalInputActive(string physicalInput)
        {
            if (string.IsNullOrWhiteSpace(physicalInput)) return false;

            // Check if it's a mouse button
            if (physicalInput.Equals("LeftMouse", StringComparison.OrdinalIgnoreCase) ||
                physicalInput.Equals("RightMouse", StringComparison.OrdinalIgnoreCase) ||
                physicalInput.Equals("MiddleMouse", StringComparison.OrdinalIgnoreCase) ||
                physicalInput.Equals("XButton1", StringComparison.OrdinalIgnoreCase) ||
                physicalInput.Equals("XButton2", StringComparison.OrdinalIgnoreCase))
            {
                return IsMouseButtonPressed(physicalInput);
            }

            // Otherwise keyboard key
            return IsKeyPressed(physicalInput);
        }
    }
}
