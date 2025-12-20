using System;
using System.Drawing;
using System.Windows.Forms;

namespace KeyboardLayoutWatcher
{
    public class TrayManager : IDisposable
    {
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _contextMenu;
        private bool _disposed = false;

        public event EventHandler ShowRequested;
        public event EventHandler ExitRequested;

        private Icon _icon;

        public TrayManager(Icon icon = null)
        {
            _icon = icon ?? SystemIcons.Application;
            CreateContextMenu();
            CreateTrayIcon();
        }

        private void CreateContextMenu()
        {
            _contextMenu = new ContextMenuStrip();
            _contextMenu.BackColor = Color.FromArgb(45, 45, 45);
            _contextMenu.ForeColor = Color.White;
            _contextMenu.Renderer = new DarkMenuRenderer();

            var showItem = new ToolStripMenuItem("Show");
            showItem.Click += (s, e) => ShowRequested?.Invoke(this, EventArgs.Empty);
            _contextMenu.Items.Add(showItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
            _contextMenu.Items.Add(exitItem);
        }

        private void CreateTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Icon = _icon,
                Text = "Keyboard Layout Watcher",
                ContextMenuStrip = _contextMenu,
                Visible = true
            };

            _trayIcon.DoubleClick += (s, e) => ShowRequested?.Invoke(this, EventArgs.Empty);
        }

        public void ShowBalloon(string title, string text, int timeout = 1000)
        {
            _trayIcon.BalloonTipTitle = title;
            _trayIcon.BalloonTipText = text;
            _trayIcon.ShowBalloonTip(timeout);
        }

        public void UpdateTooltip(string text)
        {
            // NotifyIcon.Text has a 63 character limit
            if (text.Length > 63)
                text = text.Substring(0, 60) + "...";
            _trayIcon.Text = text;
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
                if (disposing)
                {
                    _trayIcon?.Dispose();
                    _contextMenu?.Dispose();
                }
                _disposed = true;
            }
        }

        ~TrayManager()
        {
            Dispose(false);
        }
    }

    // Custom renderer for dark theme context menu
    internal class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColorTable()) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.White;
            base.OnRenderItemText(e);
        }
    }

    internal class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(60, 60, 60);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(60, 60, 60);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(60, 60, 60);
        public override Color MenuBorder => Color.FromArgb(80, 80, 80);
        public override Color MenuItemBorder => Color.FromArgb(80, 80, 80);
        public override Color ToolStripDropDownBackground => Color.FromArgb(45, 45, 45);
        public override Color ImageMarginGradientBegin => Color.FromArgb(45, 45, 45);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(45, 45, 45);
        public override Color ImageMarginGradientEnd => Color.FromArgb(45, 45, 45);
        public override Color SeparatorDark => Color.FromArgb(80, 80, 80);
        public override Color SeparatorLight => Color.FromArgb(80, 80, 80);
    }
}
