using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using VirtualController.Core;
using VirtualController.Services;

namespace VirtualController.Input
{
    public class WindowsHookManager : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_XBUTTONUP = 0x020C;

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        private const uint WM_QUIT = 0x0012;

        private readonly InputState _inputState;
        private Thread? _hookThread;
        private uint _hookThreadId;
        private IntPtr _keyboardHook = IntPtr.Zero;
        private IntPtr _mouseHook = IntPtr.Zero;

        private LowLevelProc? _keyboardProcDelegate;
        private LowLevelProc? _mouseProcDelegate;

        private bool _isFirstMousePoint = true;
        private POINT _lastMousePoint;
        private bool _disposed;

        /// <summary>
        /// When true, low-level mouse hook calculates deltas from cursor positions.
        /// Disabled by default in favor of high-precision Raw Input.
        /// </summary>
        public bool CaptureMouseMovement { get; set; } = false;

        public event Action? EmergencyDisableRequested;
        public event Action? ToggleRequested;

        public bool IsRunning => _hookThread != null && _hookThread.IsAlive;

        public WindowsHookManager(InputState inputState)
        {
            _inputState = inputState ?? throw new ArgumentNullException(nameof(inputState));
        }

        public void Start()
        {
            if (IsRunning) return;

            using var startedEvent = new ManualResetEventSlim(false);

            _hookThread = new Thread(() =>
            {
                _hookThreadId = GetCurrentThreadId();
                _keyboardProcDelegate = KeyboardHookCallback;
                _mouseProcDelegate = MouseHookCallback;

                using var curProcess = Process.GetCurrentProcess();
                using var curModule = curProcess.MainModule;
                IntPtr modHandle = GetModuleHandle(curModule?.ModuleName);

                _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProcDelegate, modHandle, 0);
                _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProcDelegate, modHandle, 0);

                if (_keyboardHook == IntPtr.Zero)
                {
                    AppLogger.Error("Failed to install low-level keyboard hook.");
                }
                if (_mouseHook == IntPtr.Zero)
                {
                    AppLogger.Error("Failed to install low-level mouse hook.");
                }

                startedEvent.Set();

                // Standard message pump for the hooks
                while (GetMessage(out MSG msg, IntPtr.Zero, 0, 0) > 0)
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }

                if (_keyboardHook != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_keyboardHook);
                    _keyboardHook = IntPtr.Zero;
                }
                if (_mouseHook != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_mouseHook);
                    _mouseHook = IntPtr.Zero;
                }
            })
            {
                IsBackground = true,
                Name = "InputHookThread"
            };

            _hookThread.SetApartmentState(ApartmentState.STA);
            _hookThread.Start();

            startedEvent.Wait(2000);
            AppLogger.Info("Windows low-level input hooks installed and active.");
        }

        public void Stop()
        {
            if (!IsRunning) return;

            if (_hookThreadId != 0)
            {
                PostThreadMessage(_hookThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            }

            _hookThread?.Join(1000);
            _hookThread = null;
            _hookThreadId = 0;
            _isFirstMousePoint = true;

            _inputState.Clear();
            AppLogger.Info("Windows low-level input hooks removed.");
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                bool isDown = (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN);
                bool isUp = (msg == WM_KEYUP || msg == WM_SYSKEYUP);

                if (isDown || isUp)
                {
                    var kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    int vkCode = (int)kbd.vkCode;

                    // Resolve specific modifiers
                    string keyName = ResolveKeyName(vkCode);
                    _inputState.SetKey(keyName, isDown);

                    // Also set general modifier names
                    if (keyName == "LeftShift" || keyName == "RightShift") _inputState.SetKey("Shift", isDown);
                    if (keyName == "LeftCtrl" || keyName == "RightCtrl") _inputState.SetKey("Ctrl", isDown);
                    if (keyName == "LeftAlt" || keyName == "RightAlt") _inputState.SetKey("Alt", isDown);

                    // Check Emergency Hotkey: Ctrl + Alt + F10
                    if (isDown && vkCode == 0x79) // VK_F10 = 0x79
                    {
                        bool isCtrl = (GetAsyncKeyState(0x11) & 0x8000) != 0; // VK_CONTROL
                        bool isAlt = (GetAsyncKeyState(0x12) & 0x8000) != 0;  // VK_MENU

                        if (isCtrl && isAlt)
                        {
                            AppLogger.Info("Emergency Hotkey detected (Ctrl + Alt + F10)!");
                            EmergencyDisableRequested?.Invoke();
                            ToggleRequested?.Invoke();
                        }
                    }
                }
            }

            return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        private static string ResolveKeyName(int vkCode)
        {
            switch (vkCode)
            {
                case 0x10:
                    return ((GetAsyncKeyState(0xA1) & 0x8000) != 0) ? "RightShift" : "LeftShift";
                case 0x11:
                    return ((GetAsyncKeyState(0xA3) & 0x8000) != 0) ? "RightCtrl" : "LeftCtrl";
                case 0x12:
                    return ((GetAsyncKeyState(0xA5) & 0x8000) != 0) ? "RightAlt" : "LeftAlt";
                default:
                    return KeyNameHelper.GetKeyName(vkCode);
            }
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                switch (msg)
                {
                    case WM_LBUTTONDOWN:
                        _inputState.SetMouseButton("LeftMouse", true);
                        break;
                    case WM_LBUTTONUP:
                        _inputState.SetMouseButton("LeftMouse", false);
                        break;

                    case WM_RBUTTONDOWN:
                        _inputState.SetMouseButton("RightMouse", true);
                        break;
                    case WM_RBUTTONUP:
                        _inputState.SetMouseButton("RightMouse", false);
                        break;

                    case WM_MBUTTONDOWN:
                        _inputState.SetMouseButton("MiddleMouse", true);
                        break;
                    case WM_MBUTTONUP:
                        _inputState.SetMouseButton("MiddleMouse", false);
                        break;

                    case WM_XBUTTONDOWN:
                        uint xbtnDown = (hookStruct.mouseData >> 16) & 0xFFFF;
                        if (xbtnDown == 1) _inputState.SetMouseButton("XButton1", true);
                        else if (xbtnDown == 2) _inputState.SetMouseButton("XButton2", true);
                        break;

                    case WM_XBUTTONUP:
                        uint xbtnUp = (hookStruct.mouseData >> 16) & 0xFFFF;
                        if (xbtnUp == 1) _inputState.SetMouseButton("XButton1", false);
                        else if (xbtnUp == 2) _inputState.SetMouseButton("XButton2", false);
                        break;

                    case WM_MOUSEMOVE:
                        if (!CaptureMouseMovement) break;
                        if (_isFirstMousePoint)
                        {
                            _lastMousePoint = hookStruct.pt;
                            _isFirstMousePoint = false;
                        }
                        else
                        {
                            int dx = hookStruct.pt.x - _lastMousePoint.x;
                            int dy = hookStruct.pt.y - _lastMousePoint.y;
                            _lastMousePoint = hookStruct.pt;

                            // Only accumulate reasonable deltas (ignore teleportation if cursor wrapped)
                            if (Math.Abs(dx) < 1000 && Math.Abs(dy) < 1000)
                            {
                                _inputState.AddMouseDelta(dx, dy);
                            }
                        }
                        break;
                }
            }

            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }
    }
}
