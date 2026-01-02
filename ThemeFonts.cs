using System.Drawing;

namespace KeyboardLayoutWatcher
{
    public static class ThemeFonts
    {
        public static Font Heading => new Font("Segoe UI", 12);
        public static Font Normal => new Font("Segoe UI", 10);
        public static Font Small => new Font("Segoe UI", 9);
        public static Font SmallUnderlined => new Font("Segoe UI", 9, FontStyle.Underline);
    }
}
