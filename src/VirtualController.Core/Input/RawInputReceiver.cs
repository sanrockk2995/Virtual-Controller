using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using VirtualController.Core;
using VirtualController.Services;

namespace VirtualController.Input
{
    /// <summary>
    /// Low-latency Windows Raw Input (WM_INPUT) receiver for relative mouse motion.
    /// Operates via a dedicated hidden window and message loop with RIDEV_INPUTSINK,
    /// capturing unaccelerated hardware mickeys without screen boundary clipping.
    /// </summary>
    public class RawInputReceiver : IDisposable
    {
        private const int WM_INPUT = 0x00FF;
        private const int WM_QUIT = 0x0012;
        private const uint RID_INPUT = 0x10000003;
        private const uint RIM_TYPEMOUSE = 0;

        private const ushort HID_USAGE_PAGE_GENERIC = 0x01;
        private const ushort HID_USAGE_GENERIC_MOUSE = 0x02;
        private const uint RIDEV_INPUTSINK = 0x00000100;
        private const uint RIDEV_REMOVE = 0x00000001;

        // Mouse button flags
        private const ushort RI_MOUSE_BUTTON_1_DOWN = 0x0001; // Left down
        private const ushort RI_MOUSE_BUTTON_1_UP = 0x0002;   // Left up
        private const ushort RI_MOUSE_BUTTON_2_DOWN = 0x0004; // Right down
        private const ushort RI_MOUSE_BUTTON_2_UP = 0x0008;   // Right up
        private const ushort RI_MOUSE_BUTTON_3_DOWN = 0x0010; // Middle down
        private const ushort RI_MOUSE_BUTTON_3_UP = 0x0020;   // Middle up
        private const ushort RI_MOUSE_BUTTON_4_DOWN = 0x0040; // X1 down
        private const ushort RI_MOUSE_BUTTON_4_UP = 0x0080;   // X1 up
        private const ushort RI_MOUSE_BUTTON_5_DOWN = 0x0100; // X2 down
        private const ushort RI_MOUSE_BUTTON_5_UP = 0x0200;   // X2 up

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWMOUSE
        {
            public ushort usFlags;
            public ushort usButtonFlags;
            public ushort usButtonData;
            public uint ulRawButtons;
            public int lLastX;
            public int lLastY;
            public uint ulExtraInformation;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct RAWINPUT
        {
            [FieldOffset(0)]
            public RAWINPUTHEADER header;
            [FieldOffset(16)] // 64-bit alignment for x64
            public RAWMOUSE mouse;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int pt_x;
            public int pt_y;
        }

        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(
            [In] RAWINPUTDEVICE[] pRawInputDevices,
            uint uiNumDevices,
            uint cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputData(
            IntPtr hRawInput,
            uint uiCommand,
            IntPtr pData,
            ref uint pcbSize,
            uint cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateWindowEx(
            uint dwExStyle,
            string lpClassName,
            string lpWindowName,
            uint dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern ushort RegisterClass(ref WNDCLASS lpWndClass);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct WNDCLASS
        {
            public uint style;
            public WndProc lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
        }

        private readonly InputState _inputState;
        private Thread? _receiverThread;
        private uint _receiverThreadId;
        private IntPtr _hwnd = IntPtr.Zero;
        private WndProc? _wndProcDelegate;
        private bool _disposed;

        public bool IsRunning => _receiverThread != null && _receiverThread.IsAlive;

        public RawInputReceiver(InputState inputState)
        {
            _inputState = inputState ?? throw new ArgumentNullException(nameof(inputState));
        }

        public void Start()
        {
            if (IsRunning) return;

            using var readyEvent = new ManualResetEventSlim(false);

            _receiverThread = new Thread(() =>
            {
                _receiverThreadId = GetCurrentThreadId();
                _wndProcDelegate = WindowProc;

                string className = "VirtualController_RawInputHost_" + Guid.NewGuid().ToString("N");
                IntPtr hInst = GetModuleHandle(null);

                var wc = new WNDCLASS
                {
                    lpfnWndProc = _wndProcDelegate,
                    hInstance = hInst,
                    lpszClassName = className
                };

                RegisterClass(ref wc);

                // Create message-only / hidden utility window
                _hwnd = CreateWindowEx(0, className, "RawInputHost", 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, hInst, IntPtr.Zero);

                if (_hwnd == IntPtr.Zero)
                {
                    AppLogger.Error("Failed to create Raw Input host window.");
                    readyEvent.Set();
                    return;
                }

                // Register mouse for raw input sink
                var rid = new RAWINPUTDEVICE[1];
                rid[0].usUsagePage = HID_USAGE_PAGE_GENERIC;
                rid[0].usUsage = HID_USAGE_GENERIC_MOUSE;
                rid[0].dwFlags = RIDEV_INPUTSINK;
                rid[0].hwndTarget = _hwnd;

                bool registered = RegisterRawInputDevices(rid, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
                if (!registered)
                {
                    int err = Marshal.GetLastWin32Error();
                    AppLogger.Error($"Failed to register Raw Input device: win32 error {err}");
                }
                else
                {
                    AppLogger.Info("Windows Raw Input mouse sink registered successfully.");
                }

                readyEvent.Set();

                // Standard message pump
                while (GetMessage(out MSG msg, IntPtr.Zero, 0, 0) > 0)
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }

                // Unregister
                rid[0].dwFlags = RIDEV_REMOVE;
                rid[0].hwndTarget = IntPtr.Zero;
                RegisterRawInputDevices(rid, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());

                if (_hwnd != IntPtr.Zero)
                {
                    DestroyWindow(_hwnd);
                    _hwnd = IntPtr.Zero;
                }
            })
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest,
                Name = "RawInputReceiverThread"
            };

            _receiverThread.SetApartmentState(ApartmentState.STA);
            _receiverThread.Start();

            readyEvent.Wait(2000);
        }

        public void Stop()
        {
            if (!IsRunning) return;

            if (_receiverThreadId != 0)
            {
                PostThreadMessage(_receiverThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            }

            _receiverThread?.Join(1000);
            _receiverThread = null;
            _receiverThreadId = 0;
            AppLogger.Info("Windows Raw Input receiver stopped.");
        }

        private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_INPUT)
            {
                ProcessRawInputMessage(lParam);
                return IntPtr.Zero;
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void ProcessRawInputMessage(IntPtr hRawInput)
        {
            uint dwSize = 0;
            uint headerSize = (uint)Marshal.SizeOf<RAWINPUTHEADER>();

            // Query buffer size
            GetRawInputData(hRawInput, RID_INPUT, IntPtr.Zero, ref dwSize, headerSize);
            if (dwSize == 0) return;

            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            try
            {
                if (GetRawInputData(hRawInput, RID_INPUT, buffer, ref dwSize, headerSize) == dwSize)
                {
                    var raw = Marshal.PtrToStructure<RAWINPUT>(buffer);
                    if (raw.header.dwType == RIM_TYPEMOUSE)
                    {
                        var mouse = raw.mouse;

                        // Process relative displacement
                        if (mouse.lLastX != 0 || mouse.lLastY != 0)
                        {
                            _inputState.AddMouseDelta(mouse.lLastX, mouse.lLastY);
                        }

                        // Process button states
                        ushort btnFlags = mouse.usButtonFlags;
                        if ((btnFlags & RI_MOUSE_BUTTON_1_DOWN) != 0) _inputState.SetMouseButton("LeftMouse", true);
                        if ((btnFlags & RI_MOUSE_BUTTON_1_UP) != 0) _inputState.SetMouseButton("LeftMouse", false);

                        if ((btnFlags & RI_MOUSE_BUTTON_2_DOWN) != 0) _inputState.SetMouseButton("RightMouse", true);
                        if ((btnFlags & RI_MOUSE_BUTTON_2_UP) != 0) _inputState.SetMouseButton("RightMouse", false);

                        if ((btnFlags & RI_MOUSE_BUTTON_3_DOWN) != 0) _inputState.SetMouseButton("MiddleMouse", true);
                        if ((btnFlags & RI_MOUSE_BUTTON_3_UP) != 0) _inputState.SetMouseButton("MiddleMouse", false);

                        if ((btnFlags & RI_MOUSE_BUTTON_4_DOWN) != 0) _inputState.SetMouseButton("XButton1", true);
                        if ((btnFlags & RI_MOUSE_BUTTON_4_UP) != 0) _inputState.SetMouseButton("XButton1", false);

                        if ((btnFlags & RI_MOUSE_BUTTON_5_DOWN) != 0) _inputState.SetMouseButton("XButton2", true);
                        if ((btnFlags & RI_MOUSE_BUTTON_5_UP) != 0) _inputState.SetMouseButton("XButton2", false);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }
    }
}
