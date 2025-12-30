using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace KeyboardLayoutWatcher
{
    public partial class Form1 : Form
    {
        // P/Invoke declarations for keyboard layout detection
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

        // New components
        private KeyboardHook _keyboardHook;
        private TrayManager _trayManager;
        private RadioButton _rbBlockCompletely;
        private RadioButton _rbAllowMultiPress;
        private NumericUpDown _nudPressCount;
        private Label _lblTimes;
        private CheckBox _chkShowAlert;
        private CheckBox _chkMinimizeOnStart;
        private CheckBox _chkMinimizeToTray;
        private CheckBox _chkLaunchOnStartup;
        private ToolTip _statusToolTip;
        private Timer _tooltipTimer;
        private bool _isLoading = true;
        private bool _isRestoring = false;

        public Form1()
        {
            InitializeComponent();
            InitializeUI();
            InitializeComponents();
            LoadSettings();
            ApplyStartupBehavior();
        }

        private void InitializeUI()
        {
            this.Text = "Keyboard Layout Watcher";
            this.Width = 400;
            this.Height = 276;

            // Load application icon from embedded resource
            this.Icon = Properties.Resources.icon;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Layout display label
            _layoutLabel = new Label
            {
                Location = new Point(15, 10),
                Size = new Size(370, 30),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 30, 30)
            };
            this.Controls.Add(_layoutLabel);

            // Separator
            var separator = new Label
            {
                Location = new Point(10, 45),
                Size = new Size(370, 1),
                BackColor = Color.FromArgb(60, 60, 60)
            };
            this.Controls.Add(separator);

            // Win+Space options - Radio buttons
            int yPos = 55;
            int spacing = 28;

            _rbBlockCompletely = new RadioButton
            {
                Text = "Block Win+Space completely",
                Location = new Point(15, yPos),
                Size = new Size(360, 24),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 30, 30),
                FlatStyle = FlatStyle.Flat
            };
            _rbBlockCompletely.CheckedChanged += RadioButton_CheckedChanged;
            this.Controls.Add(_rbBlockCompletely);
            yPos += spacing;

            _rbAllowMultiPress = new RadioButton
            {
                Text = "Allow Win+Space after pressing",
                Location = new Point(15, yPos),
                Size = new Size(230, 24),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 30, 30),
                FlatStyle = FlatStyle.Flat
            };
            _rbAllowMultiPress.CheckedChanged += RadioButton_CheckedChanged;
            this.Controls.Add(_rbAllowMultiPress);

            _nudPressCount = new NumericUpDown
            {
                Location = new Point(250, yPos),
                Size = new Size(45, 24),
                Minimum = 2,
                Maximum = 5,
                Value = 3,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White
            };
            _nudPressCount.ValueChanged += NudPressCount_ValueChanged;
            this.Controls.Add(_nudPressCount);

            _lblTimes = new Label
            {
                Text = "times",
                Location = new Point(300, yPos + 2),
                Size = new Size(50, 24),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 30, 30)
            };
            this.Controls.Add(_lblTimes);
            yPos += spacing;

            // Other checkboxes
            _chkShowAlert = CreateCheckBox("Show alert on layout change", yPos);
            yPos += spacing;

            _chkMinimizeOnStart = CreateCheckBox("Minimize on start", yPos);
            yPos += spacing;

            _chkMinimizeToTray = CreateCheckBox("Minimize to tray", yPos);
            yPos += spacing;

            _chkLaunchOnStartup = CreateCheckBox("Launch on Windows startup", yPos);

            // Status tooltip
            _statusToolTip = new ToolTip
            {
                AutoPopDelay = 1000,
                InitialDelay = 0,
                ReshowDelay = 0,
                ShowAlways = true
            };

            _tooltipTimer = new Timer { Interval = 1000 };
            _tooltipTimer.Tick += (s, e) =>
            {
                _statusToolTip.Hide(this);
                _tooltipTimer.Stop();
            };
        }

        private CheckBox CreateCheckBox(string text, int yPos)
        {
            var checkBox = new CheckBox
            {
                Text = text,
                Location = new Point(15, yPos),
                Size = new Size(360, 24),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 30, 30),
                FlatStyle = FlatStyle.Flat
            };
            checkBox.CheckedChanged += CheckBox_CheckedChanged;
            this.Controls.Add(checkBox);
            return checkBox;
        }

        private void InitializeComponents()
        {
            // Initialize keyboard hook
            _keyboardHook = new KeyboardHook();
            _keyboardHook.StatusChanged += KeyboardHook_StatusChanged;
            _keyboardHook.Install();

            // Initialize tray manager
            _trayManager = new TrayManager(this.Icon);
            _trayManager.ShowRequested += (s, e) =>
            {
                Log("ShowRequested event fired");
                RestoreFromTray();
            };
            _trayManager.ExitRequested += (s, e) => Application.Exit();

            // Timer for layout polling
            _timer = new Timer { Interval = 200 };
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

        private void LoadSettings()
        {
            _isLoading = true;
            var settings = AppSettings.Instance;

            // Win+Space options
            _rbBlockCompletely.Checked = settings.WinSpaceBlockCompletely;
            _rbAllowMultiPress.Checked = !settings.WinSpaceBlockCompletely;
            _nudPressCount.Value = Math.Max(2, Math.Min(5, settings.WinSpacePressCount));
            _nudPressCount.Enabled = !settings.WinSpaceBlockCompletely;
            _lblTimes.Enabled = !settings.WinSpaceBlockCompletely;

            _chkShowAlert.Checked = settings.ShowAlertOnLayoutChange;
            _chkMinimizeOnStart.Checked = settings.MinimizeOnStart;
            _chkMinimizeToTray.Checked = settings.MinimizeToTray;
            _chkLaunchOnStartup.Checked = IsStartupEnabled();

            _keyboardHook.BlockCompletely = settings.WinSpaceBlockCompletely;
            _keyboardHook.RequiredPressCount = settings.WinSpacePressCount;
            _isLoading = false;
        }

        private void ApplyStartupBehavior()
        {
            if (AppSettings.Instance.MinimizeOnStart)
            {
                this.WindowState = FormWindowState.Minimized;
                if (AppSettings.Instance.MinimizeToTray)
                {
                    this.ShowInTaskbar = false;
                    this.Hide();
                }
            }
        }

        private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string StartupValueName = "KeyboardLayoutWatcher";

        private bool IsStartupEnabled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false))
            {
                return key?.GetValue(StartupValueName) != null;
            }
        }

        private void SetStartupEnabled(bool enable)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true))
            {
                if (key == null) return;

                if (enable)
                {
                    key.SetValue(StartupValueName, Application.ExecutablePath);
                }
                else
                {
                    key.DeleteValue(StartupValueName, false);
                }
            }
        }

        private void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;

            var settings = AppSettings.Instance;
            settings.ShowAlertOnLayoutChange = _chkShowAlert.Checked;
            settings.MinimizeOnStart = _chkMinimizeOnStart.Checked;
            settings.MinimizeToTray = _chkMinimizeToTray.Checked;
            settings.LaunchOnStartup = _chkLaunchOnStartup.Checked;
            settings.Save();

            SetStartupEnabled(_chkLaunchOnStartup.Checked);
        }

        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;

            var settings = AppSettings.Instance;
            settings.WinSpaceBlockCompletely = _rbBlockCompletely.Checked;
            settings.Save();

            _keyboardHook.BlockCompletely = settings.WinSpaceBlockCompletely;

            // Enable/disable NumericUpDown based on selection
            _nudPressCount.Enabled = _rbAllowMultiPress.Checked;
            _lblTimes.Enabled = _rbAllowMultiPress.Checked;
        }

        private void NudPressCount_ValueChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;

            var settings = AppSettings.Instance;
            settings.WinSpacePressCount = (int)_nudPressCount.Value;
            settings.Save();

            _keyboardHook.RequiredPressCount = settings.WinSpacePressCount;
        }

        private void KeyboardHook_StatusChanged(object sender, string status)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => ShowStatusTooltip(status)));
            }
            else
            {
                ShowStatusTooltip(status);
            }
        }

        private void ShowStatusTooltip(string status)
        {
            _tooltipTimer.Stop();
            var screen = Screen.FromControl(this);
            var cursorPos = Cursor.Position;
            _statusToolTip.Show(status, this, this.PointToClient(cursorPos), 1000);
            _tooltipTimer.Start();
        }

        private void UpdateLayoutDisplay(IntPtr layout)
        {
            if (layout == _lastLayout)
                return;

            _lastLayout = layout;

            int langId = (ushort)layout.ToInt64();
            int keyboardId = (ushort)(layout.ToInt64() >> 16);
            string klid = keyboardId.ToString("X4").PadLeft(8, '0');

            CultureInfo culture = null;
            try
            {
                culture = new CultureInfo(langId);
            }
            catch { }

            string keyboardLayoutName;
            if (KeyboardLayoutNames.TryGetValue(klid, out string friendlyName))
            {
                keyboardLayoutName = friendlyName;
            }
            else
            {
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
            _trayManager?.UpdateTooltip(text);

            if (_initialLayoutSet && AppSettings.Instance.ShowAlertOnLayoutChange)
            {
                ShowLayoutAlert(keyboardLayoutName);
            }
            _initialLayoutSet = true;
        }

        private void ShowLayoutAlert(string layoutName)
        {
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
                IntPtr newLayout = m.LParam;
                UpdateLayoutDisplay(newLayout);
            }

            base.WndProc(ref m);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (!_isRestoring && this.WindowState == FormWindowState.Minimized && AppSettings.Instance.MinimizeToTray)
            {
                this.ShowInTaskbar = false;
                this.Hide();
            }
        }

        private void RestoreFromTray()
        {
            Log("RestoreFromTray called");
            Log($"  IsHandleCreated: {this.IsHandleCreated}");
            Log($"  InvokeRequired: {this.InvokeRequired}");
            Log($"  Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            try
            {
                if (this.InvokeRequired)
                {
                    Log("  InvokeRequired=true, using BeginInvoke");
                    this.BeginInvoke(new Action(() =>
                    {
                        Log("  Inside BeginInvoke callback");
                        DoRestoreFromTray();
                    }));
                    return;
                }

                DoRestoreFromTray();
            }
            catch (Exception ex)
            {
                Log($"RestoreFromTray error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void DoRestoreFromTray()
        {
            Log("DoRestoreFromTray called");
            _isRestoring = true;
            try
            {
                Log("  Setting WindowState=Normal");
                this.WindowState = FormWindowState.Normal;
                Log("  Setting ShowInTaskbar=true");
                this.ShowInTaskbar = true;
                Log("  Setting Visible=true");
                this.Visible = true;
                Log("  Calling Activate");
                this.Activate();
                Log("  Calling BringToFront");
                this.BringToFront();
                Log("DoRestoreFromTray complete");
            }
            catch (Exception ex)
            {
                Log($"DoRestoreFromTray error: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _isRestoring = false;
            }
        }

        private const bool DEBUG_LOGGING = false; // Set to true to enable debug logging

        private void Log(string message)
        {
            if (!DEBUG_LOGGING) return;

            string logPath = System.IO.Path.Combine(Application.StartupPath, "debug.log");
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            System.IO.File.AppendAllText(logPath, line + Environment.NewLine);
            System.Diagnostics.Debug.WriteLine(line);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _timer?.Stop();
            _timer?.Dispose();
            _keyboardHook?.Dispose();
            _trayManager?.Dispose();
            _tooltipTimer?.Stop();
            _tooltipTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
