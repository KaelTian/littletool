namespace _005Tools
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    // 移到类外的枚举和事件参数，避免作用域问题
    [Flags]
    public enum ModifierKeys
    {
        None = 0,
        Control = 1,
        Shift = 2,
        Alt = 4,
        Win = 8
    }

    public class GlobalKeyEventArgs : EventArgs
    {
        public Keys Key { get; }
        public bool IsRepeat { get; }
        public ModifierKeys Modifiers { get; }
        public bool Handled { get; set; }

        public bool Control => (Modifiers & ModifierKeys.Control) != 0;
        public bool Shift => (Modifiers & ModifierKeys.Shift) != 0;
        public bool Alt => (Modifiers & ModifierKeys.Alt) != 0;
        public bool Windows => (Modifiers & ModifierKeys.Win) != 0;

        internal GlobalKeyEventArgs(Keys key, bool isRepeat, ModifierKeys modifiers)
        {
            Key = key;
            IsRepeat = isRepeat;
            Modifiers = modifiers;
        }

        public bool IsHotkey(Keys key, ModifierKeys modifiers) =>
            Key == key && Modifiers == modifiers;
    }

    public class GlobalMouseEventArgs : EventArgs
    {
        public MouseButtons Button { get; }
        public Point Location { get; }
        public int Clicks { get; }
        public int Delta { get; }
        public ModifierKeys Modifiers { get; }
        public bool Handled { get; set; }
        public bool IsPressed { get; internal set; }

        // 添加这些便捷属性 ↓↓↓
        public bool Control => (Modifiers & ModifierKeys.Control) != 0;
        public bool Shift => (Modifiers & ModifierKeys.Shift) != 0;
        public bool Alt => (Modifiers & ModifierKeys.Alt) != 0;
        public bool Windows => (Modifiers & ModifierKeys.Win) != 0;

        // 原有的按钮检查属性
        public bool LeftButton => Button == MouseButtons.Left;
        public bool RightButton => Button == MouseButtons.Right;
        public bool MiddleButton => Button == MouseButtons.Middle;
        public bool IsWheel => Delta != 0;

        internal GlobalMouseEventArgs(MouseButtons button, Point location, int clicks, int delta, ModifierKeys modifiers)
        {
            Button = button;
            Location = location;
            Clicks = clicks;
            Delta = delta;
            Modifiers = modifiers;
        }

        public override string ToString() =>
            $"[{Button}] at ({Location.X}, {Location.Y}) {(IsWheel ? $"Wheel:{Delta}" : "")}";
    }

    public sealed class GlobalInputHook : IDisposable
    {
        #region Windows API
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys vKey);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

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
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_XBUTTONUP = 0x020C;
        private const int XBUTTON1 = 0x0001;
        private const int XBUTTON2 = 0x0002;

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
        #endregion

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);
        private readonly LowLevelProc _keyboardProc;
        private readonly LowLevelProc _mouseProc;
        private IntPtr _keyboardHookID = IntPtr.Zero;
        private IntPtr _mouseHookID = IntPtr.Zero;
        private bool _disposed;

        private readonly HashSet<Keys> _pressedKeys = new HashSet<Keys>();
        private readonly HashSet<MouseButtons> _pressedMouseButtons = new HashSet<MouseButtons>();

        public event EventHandler<GlobalKeyEventArgs>? KeyPressed;
        public event EventHandler<GlobalKeyEventArgs>? KeyReleased;
        public event EventHandler<GlobalMouseEventArgs>? MouseDown;
        public event EventHandler<GlobalMouseEventArgs>? MouseUp;
        public event EventHandler<GlobalMouseEventArgs>? MouseMove;
        public event EventHandler<GlobalMouseEventArgs>? MouseWheel;
        public event EventHandler<GlobalMouseEventArgs>? MouseClick;

        public GlobalInputHook(bool hookKeyboard = true, bool hookMouse = true)
        {
            if (hookKeyboard)
            {
                _keyboardProc = KeyboardHookCallback;
                _keyboardHookID = SetHook(WH_KEYBOARD_LL, _keyboardProc);
            }

            if (hookMouse)
            {
                _mouseProc = MouseHookCallback;
                _mouseHookID = SetHook(WH_MOUSE_LL, _mouseProc);
            }
        }

        private IntPtr SetHook(int idHook, LowLevelProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                var hook = SetWindowsHookEx(idHook, proc,
                    GetModuleHandle(curModule?.ModuleName ?? string.Empty), 0);

                if (hook == IntPtr.Zero)
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(),
                        $"安装{(idHook == WH_KEYBOARD_LL ? "键盘" : "鼠标")}钩子失败");

                return hook;
            }
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && !_disposed)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    bool isRepeat = _pressedKeys.Contains(key);
                    _pressedKeys.Add(key);

                    var args = new GlobalKeyEventArgs(key, isRepeat, GetCurrentModifiers());
                    KeyPressed?.Invoke(this, args);

                    if (args.Handled) return (IntPtr)1;
                }
                else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                {
                    _pressedKeys.Remove(key);
                    var args = new GlobalKeyEventArgs(key, false, GetCurrentModifiers());
                    KeyReleased?.Invoke(this, args);

                    if (args.Handled) return (IntPtr)1;
                }
            }
            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && !_disposed)
            {
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                var location = new Point(hookStruct.pt.x, hookStruct.pt.y);
                var button = GetMouseButton((int)wParam, hookStruct.mouseData);

                var args = new GlobalMouseEventArgs(
                    button,
                    location,
                    GetMouseClicks(wParam),
                    GetMouseDelta(wParam, hookStruct.mouseData),
                    GetCurrentModifiers()
                );

                switch ((int)wParam)
                {
                    case WM_LBUTTONDOWN:
                    case WM_RBUTTONDOWN:
                    case WM_MBUTTONDOWN:
                    case WM_XBUTTONDOWN:
                        _pressedMouseButtons.Add(button);
                        args.IsPressed = true;
                        MouseDown?.Invoke(this, args);
                        break;

                    case WM_LBUTTONUP:
                    case WM_RBUTTONUP:
                    case WM_MBUTTONUP:
                    case WM_XBUTTONUP:
                        _pressedMouseButtons.Remove(button);
                        args.IsPressed = false;
                        MouseUp?.Invoke(this, args);
                        MouseClick?.Invoke(this, args);
                        break;

                    case WM_MOUSEMOVE:
                        MouseMove?.Invoke(this, args);
                        break;

                    case WM_MOUSEWHEEL:
                        MouseWheel?.Invoke(this, args);
                        break;
                }

                if (args.Handled) return (IntPtr)1;
            }
            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        private MouseButtons GetMouseButton(int wParam, uint mouseData)
        {
            return wParam switch
            {
                WM_LBUTTONDOWN or WM_LBUTTONUP => MouseButtons.Left,
                WM_RBUTTONDOWN or WM_RBUTTONUP => MouseButtons.Right,
                WM_MBUTTONDOWN or WM_MBUTTONUP => MouseButtons.Middle,
                WM_XBUTTONDOWN or WM_XBUTTONUP =>
                    ((int)(mouseData >> 16) & 0xFFFF) == XBUTTON1 ? MouseButtons.XButton1 : MouseButtons.XButton2,
                _ => MouseButtons.None
            };
        }

        private int GetMouseClicks(IntPtr wParam) =>
            (wParam == (IntPtr)WM_LBUTTONDOWN || wParam == (IntPtr)WM_RBUTTONDOWN) ? 1 : 0;

        private int GetMouseDelta(IntPtr wParam, uint mouseData)
        {
            if ((int)wParam == WM_MOUSEWHEEL)
                return (short)((mouseData >> 16) & 0xFFFF);
            return 0;
        }

        private ModifierKeys GetCurrentModifiers()
        {
            var modifiers = ModifierKeys.None;
            if (IsKeyPressed(Keys.ControlKey) || IsKeyPressed(Keys.LControlKey) || IsKeyPressed(Keys.RControlKey))
                modifiers |= ModifierKeys.Control;
            if (IsKeyPressed(Keys.ShiftKey) || IsKeyPressed(Keys.LShiftKey) || IsKeyPressed(Keys.RShiftKey))
                modifiers |= ModifierKeys.Shift;
            if (IsKeyPressed(Keys.Menu) || IsKeyPressed(Keys.LMenu) || IsKeyPressed(Keys.RMenu))
                modifiers |= ModifierKeys.Alt;
            if (IsKeyPressed(Keys.LWin) || IsKeyPressed(Keys.RWin))
                modifiers |= ModifierKeys.Win;
            return modifiers;
        }

        private bool IsKeyPressed(Keys key) => (GetAsyncKeyState(key) & 0x8000) != 0;

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_keyboardHookID != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_keyboardHookID);
                    _keyboardHookID = IntPtr.Zero;
                }
                if (_mouseHookID != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_mouseHookID);
                    _mouseHookID = IntPtr.Zero;
                }
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        ~GlobalInputHook()
        {
            Dispose();
        }
    }
}
