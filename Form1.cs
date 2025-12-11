using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyboardLayoutWatcher
{
    public partial class Form1 : Form
    {
        // P/Invoke declarations
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);


        // Windows message constants
        private const int WM_INPUTLANGCHANGE = 0x0051;

        // Common keyboard layout identifiers
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

        private IntPtr _lastLayout = IntPtr.Zero;
        private Label _layoutLabel;
        private Timer _timer;
        private bool _initialLayoutSet = false;
        private Form _currentAlert = null;

        public Form1()
        {
            InitializeComponent();

            // Optional: make the window small & unobtrusive
            this.Text = "Keyboard Layout Watcher";
            this.Width = 400;
            this.Height = 80;
            this.BackColor = Color.FromArgb(30, 30, 30);

            // Create a label to show the current layout
            _layoutLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 30, 30)
            };
            this.Controls.Add(_layoutLabel);

            // Timer to poll the current layout of the foreground window (fallback)
            _timer = new Timer();
            _timer.Interval = 200; // ms, adjust as you like
            _timer.Tick += (s, e) =>
            {
                IntPtr foreground = GetForegroundWindow();
                if (foreground == IntPtr.Zero) return;

                uint threadId = GetWindowThreadProcessId(foreground, IntPtr.Zero);
                IntPtr layout = GetKeyboardLayout(threadId);

                UpdateLayoutDisplay(layout);
            };

            _timer.Start();
        }

        private void UpdateLayoutDisplay(IntPtr layout)
        {
            if (layout == _lastLayout)
                return;

            _lastLayout = layout;

            // Extract LANGID from HKL (low word) for language info
            int langId = (ushort)layout.ToInt64();

            // Extract keyboard layout ID from HKL (high word)
            int keyboardId = (ushort)(layout.ToInt64() >> 16);

            // Build KLID from high word for dictionary lookup
            string klid = keyboardId.ToString("X4").PadLeft(8, '0');

            CultureInfo culture = null;
            try
            {
                culture = new CultureInfo(langId);
            }
            catch
            {
                // Fallback if something weird happens
            }

            // Try to get friendly keyboard layout name from high word
            string keyboardLayoutName;
            if (KeyboardLayoutNames.TryGetValue(klid, out string friendlyName))
            {
                keyboardLayoutName = friendlyName;
            }
            else
            {
                // Fallback: show the raw KLID
                keyboardLayoutName = klid;
            }

            string text;
            if (culture != null)
            {
                text = $"Layout: {keyboardLayoutName} | Lang: {culture.DisplayName}";
            }
            else
            {
                text = $"Layout: {keyboardLayoutName} | HKL: 0x{layout.ToInt64():X}";
            }

            _layoutLabel.Text = text;
            this.Text = text;

            // Show alert popup (skip on initial detection)
            if (_initialLayoutSet)
            {
                ShowLayoutAlert(keyboardLayoutName);
            }
            _initialLayoutSet = true;
        }

        private void ShowLayoutAlert(string layoutName)
        {
            // Close any existing alert
            if (_currentAlert != null && !_currentAlert.IsDisposed)
            {
                _currentAlert.Close();
                _currentAlert.Dispose();
            }

            _currentAlert = new Form
            {
                Text = "Layout Changed",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                Size = new Size(300, 150),
                MaximizeBox = false,
                MinimizeBox = false,
                TopMost = true,
                ShowInTaskbar = false,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            var label = new Label
            {
                Text = $"Keyboard layout changed to:\n{layoutName}",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 30, 30)
            };
            _currentAlert.Controls.Add(label);

            var okButton = new Button
            {
                Text = "OK",
                Size = new Size(75, 30),
                Location = new Point(112, 80),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            okButton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            okButton.Click += (s, e) => _currentAlert.Close();
            _currentAlert.Controls.Add(okButton);
            _currentAlert.AcceptButton = okButton;
            _currentAlert.KeyPreview = true;
            _currentAlert.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    _currentAlert.Close();
            };
            okButton.Focus();

            _currentAlert.Show();
            _currentAlert.Activate();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_INPUTLANGCHANGE)
            {
                // lParam contains the new input locale identifier (HKL)
                IntPtr newLayout = m.LParam;
                UpdateLayoutDisplay(newLayout);
            }

            base.WndProc(ref m);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _timer?.Stop();
            _timer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
