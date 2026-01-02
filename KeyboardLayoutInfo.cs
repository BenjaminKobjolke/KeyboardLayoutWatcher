using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace KeyboardLayoutWatcher
{
    public static class KeyboardLayoutInfo
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        private static readonly Dictionary<string, string> KeyboardLayoutNames = new Dictionary<string, string>
        {
            { "00000409", "US" },
            { "00000407", "German" },
            { "00000809", "UK" },
            { "0000040C", "French" },
            { "00000410", "Italian" },
            { "00000C0C", "Canadian French" },
            { "00000416", "Portuguese (Brazil)" },
            { "00000816", "Portuguese (Portugal)" },
            { "0000040A", "Spanish" },
            { "00000413", "Dutch" },
            { "0000041D", "Swedish" },
            { "00000414", "Norwegian" },
            { "0000040B", "Finnish" },
            { "00000406", "Danish" },
            { "00000415", "Polish" },
            { "00000405", "Czech" },
            { "0000041B", "Slovak" },
            { "0000040E", "Hungarian" },
            { "00000418", "Romanian" },
            { "00000402", "Bulgarian" },
            { "00000419", "Russian" },
            { "00000422", "Ukrainian" },
            { "00000408", "Greek" },
            { "0000041F", "Turkish" },
            { "0000040D", "Hebrew" },
            { "00000401", "Arabic" },
            { "00000411", "Japanese" },
            { "00000412", "Korean" },
            { "00000404", "Chinese (Traditional)" },
            { "00000804", "Chinese (Simplified)" },
        };

        public static IntPtr GetCurrentLayout()
        {
            IntPtr foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero) return IntPtr.Zero;

            uint threadId = GetWindowThreadProcessId(foreground, IntPtr.Zero);
            return GetKeyboardLayout(threadId);
        }

        public static string GetLayoutName(IntPtr layout)
        {
            int keyboardId = (ushort)(layout.ToInt64() >> 16);
            string klid = keyboardId.ToString("X4").PadLeft(8, '0');

            if (KeyboardLayoutNames.TryGetValue(klid, out string friendlyName))
            {
                return friendlyName;
            }
            return klid;
        }

        public static CultureInfo GetCultureInfo(IntPtr layout)
        {
            int langId = (ushort)layout.ToInt64();
            try
            {
                return new CultureInfo(langId);
            }
            catch
            {
                return null;
            }
        }
    }
}
