using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyboardLayoutWatcher
{
    public class KeyboardHook : IDisposable
    {
        // P/Invoke declarations
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int VK_SPACE = 0x20;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelKeyboardProc _proc;
        private bool _disposed = false;

        // Triple-press detection
        private int _pressCount = 0;
        private int _lastPressTime = 0;
        private const int PRESS_TIMEOUT = 300; // milliseconds

        // Events
        public event EventHandler<string> StatusChanged;

        public bool IsEnabled { get; set; } = true;

        public KeyboardHook()
        {
            _proc = HookCallback;
        }

        public void Install()
        {
            if (_hookId != IntPtr.Zero) return;

            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public void Uninstall()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && IsEnabled)
            {
                int msg = wParam.ToInt32();
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    KBDLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                    // Check if Space is pressed
                    if (hookStruct.vkCode == VK_SPACE)
                    {
                        // Check if Win key is held down
                        bool winKeyDown = (GetAsyncKeyState(VK_LWIN) & 0x8000) != 0 ||
                                         (GetAsyncKeyState(VK_RWIN) & 0x8000) != 0;

                        if (winKeyDown)
                        {
                            return HandleWinSpace();
                        }
                    }
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private IntPtr HandleWinSpace()
        {
            int currentTime = Environment.TickCount;

            // Reset counter if timeout exceeded
            if (currentTime - _lastPressTime > PRESS_TIMEOUT)
            {
                _pressCount = 0;
            }

            _lastPressTime = currentTime;
            _pressCount++;

            if (_pressCount >= 3)
            {
                _pressCount = 0;
                StatusChanged?.Invoke(this, "Switching...");
                // Allow the keypress through
                return CallNextHookEx(_hookId, 0, IntPtr.Zero, IntPtr.Zero);
            }
            else
            {
                StatusChanged?.Invoke(this, $"no! ({_pressCount})");
                // Block the keypress by returning non-zero
                return (IntPtr)1;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                Uninstall();
                _disposed = true;
            }
        }

        ~KeyboardHook()
        {
            Dispose(false);
        }
    }
}
