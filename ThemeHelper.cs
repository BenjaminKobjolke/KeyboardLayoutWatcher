using System.Drawing;
using Microsoft.Win32;

namespace KeyboardLayoutWatcher
{
    public static class ThemeHelper
    {
        public static bool IsWindowsUsingLightTheme()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", false))
                {
                    var value = key?.GetValue("AppsUseLightTheme");
                    return value != null && (int)value == 1;
                }
            }
            catch
            {
                return true; // Default to light theme (dark icon) if detection fails
            }
        }

        public static Icon GetThemeAppropriateIcon(Icon lightThemeIcon, Icon darkThemeIcon)
        {
            // Light theme = light taskbar background (use dark icon)
            // Dark theme = dark taskbar background (use light icon)
            return IsWindowsUsingLightTheme() ? lightThemeIcon : darkThemeIcon;
        }
    }
}
