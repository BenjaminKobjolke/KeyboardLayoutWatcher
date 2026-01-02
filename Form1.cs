using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
using CSharpLocalization;
using KeyboardLayoutWatcher.Config;

namespace KeyboardLayoutWatcher
{
    public partial class Form1 : Form
    {
        private const int WM_INPUTLANGCHANGE = 0x0051;
        private const int WM_SETTINGCHANGE = 0x001A;

        private IntPtr _lastLayout = IntPtr.Zero;
        private Timer _timer;
        private bool _initialLayoutSet = false;

        private KeyboardHook _keyboardHook;
        private TrayManager _trayManager;
        private MainFormControls _controls;
        private bool _isLoading = true;
        private bool _isRestoring = false;
        private Localization _localization;
        private string _pendingLanguageSelection;

        public Form1()
        {
            InitializeComponent();
            InitializeLocalization();
            InitializeUI();
            InitializeComponents();
            LoadSettings();
            ApplyStartupBehavior();
            this.Shown += Form1_Shown;
        }

        private void InitializeLocalization()
        {
            _localization = new Localization(new LocalizationConfig
            {
                UseEmbeddedResources = true,
                ResourceAssembly = Assembly.GetExecutingAssembly(),
                ResourcePrefix = "KeyboardLayoutWatcher.lang.",
                DefaultLang = AppSettings.Instance.Language,
                FallbackLang = "en"
            });
        }

        private void InitializeUI()
        {
            this.Text = _localization.Lang("app.title");
            this.Width = 400;
            this.Height = 340;
            this.BackColor = ThemeColors.DarkBackground;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            _controls = MainFormUI.CreateControls(this, _localization);

            // Wire up event handlers
            _controls.RbBlockCompletely.CheckedChanged += RadioButton_CheckedChanged;
            _controls.RbAllowMultiPress.CheckedChanged += RadioButton_CheckedChanged;
            _controls.NudPressCount.ValueChanged += NudPressCount_ValueChanged;
            _controls.ChkShowAlert.CheckedChanged += CheckBox_CheckedChanged;
            _controls.ChkMinimizeOnStart.CheckedChanged += CheckBox_CheckedChanged;
            _controls.ChkMinimizeToTray.CheckedChanged += CheckBox_CheckedChanged;
            _controls.ChkLaunchOnStartup.CheckedChanged += CheckBox_CheckedChanged;

            _controls.TooltipTimer.Tick += (s, e) =>
            {
                _controls.StatusToolTip.Hide(this);
                _controls.TooltipTimer.Stop();
            };
        }

        private void InitializeComponents()
        {
            _keyboardHook = new KeyboardHook(_localization);
            _keyboardHook.StatusChanged += KeyboardHook_StatusChanged;
            _keyboardHook.Install();

            _trayManager = new TrayManager(this.Icon, _localization);
            _trayManager.ShowRequested += (s, e) => RestoreFromTray();
            _trayManager.ExitRequested += (s, e) => Application.Exit();
            UpdateTrayIconForTheme();
            UpdateWindowIconForTheme();

            _timer = new Timer { Interval = 200 };
            _timer.Tick += (s, e) =>
            {
                IntPtr layout = KeyboardLayoutInfo.GetCurrentLayout();
                if (layout != IntPtr.Zero)
                {
                    UpdateLayoutDisplay(layout);
                }
            };
            _timer.Start();
        }

        private void LoadSettings()
        {
            _isLoading = true;
            var settings = AppSettings.Instance;

            _controls.RbBlockCompletely.Checked = settings.WinSpaceBlockCompletely;
            _controls.RbAllowMultiPress.Checked = !settings.WinSpaceBlockCompletely;
            _controls.NudPressCount.Value = Math.Max(2, Math.Min(5, settings.WinSpacePressCount));
            _controls.NudPressCount.Enabled = !settings.WinSpaceBlockCompletely;

            _controls.ChkShowAlert.Checked = settings.ShowAlertOnLayoutChange;
            _controls.ChkMinimizeOnStart.Checked = settings.MinimizeOnStart;
            _controls.ChkMinimizeToTray.Checked = settings.MinimizeToTray;
            _controls.ChkLaunchOnStartup.Checked = IsStartupEnabled();

            _pendingLanguageSelection = AppSettings.Instance.Language ?? _localization.CurrentLanguage;

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

        private void Form1_Shown(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_pendingLanguageSelection))
            {
                var languages = _controls.CmbLanguage.DataSource as System.Collections.IList;
                if (languages != null)
                {
                    for (int i = 0; i < languages.Count; i++)
                    {
                        dynamic lang = languages[i];
                        if (lang != null && lang.Code == _pendingLanguageSelection)
                        {
                            _controls.CmbLanguage.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            _controls.CmbLanguage.SelectedIndexChanged += CmbLanguage_SelectedIndexChanged;
        }

        private bool IsStartupEnabled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(Constants.StartupRegistryKey, false))
            {
                return key?.GetValue(Constants.StartupValueName) != null;
            }
        }

        private void SetStartupEnabled(bool enable)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(Constants.StartupRegistryKey, true))
            {
                if (key == null) return;

                if (enable)
                {
                    key.SetValue(Constants.StartupValueName, Application.ExecutablePath);
                }
                else
                {
                    key.DeleteValue(Constants.StartupValueName, false);
                }
            }
        }

        private void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;

            var settings = AppSettings.Instance;
            settings.ShowAlertOnLayoutChange = _controls.ChkShowAlert.Checked;
            settings.MinimizeOnStart = _controls.ChkMinimizeOnStart.Checked;
            settings.MinimizeToTray = _controls.ChkMinimizeToTray.Checked;
            settings.LaunchOnStartup = _controls.ChkLaunchOnStartup.Checked;
            settings.Save();

            SetStartupEnabled(_controls.ChkLaunchOnStartup.Checked);
        }

        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;

            var settings = AppSettings.Instance;
            settings.WinSpaceBlockCompletely = _controls.RbBlockCompletely.Checked;
            settings.Save();

            _keyboardHook.BlockCompletely = settings.WinSpaceBlockCompletely;
            _controls.NudPressCount.Enabled = _controls.RbAllowMultiPress.Checked;
        }

        private void NudPressCount_ValueChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;

            var settings = AppSettings.Instance;
            settings.WinSpacePressCount = (int)_controls.NudPressCount.Value;
            settings.Save();

            _keyboardHook.RequiredPressCount = settings.WinSpacePressCount;
        }

        private void UpdateTrayIconForTheme()
        {
            Icon iconToUse = ThemeHelper.GetThemeAppropriateIcon(
                Properties.Resources.icon_dark,
                Properties.Resources.icon_light);
            _trayManager?.UpdateIcon(iconToUse);
        }

        private void UpdateWindowIconForTheme()
        {
            this.Icon = ThemeHelper.GetThemeAppropriateIcon(
                Properties.Resources.icon_dark,
                Properties.Resources.icon_light);
        }

        private void CmbLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;

            var selectedLanguage = _controls.CmbLanguage.SelectedItem as LanguageInfo;
            if (selectedLanguage != null && selectedLanguage.Code != _localization.CurrentLanguage)
            {
                AppSettings.Instance.Language = selectedLanguage.Code;
                AppSettings.Instance.Save();

                _localization.SetLanguage(selectedLanguage.Code);
                RefreshLocalizedUI();
            }
        }

        private void RefreshLocalizedUI()
        {
            this.Text = _localization.Lang("app.title");

            _controls.RbBlockCompletely.Text = _localization.Lang("settings.block_completely");
            _controls.RbAllowMultiPress.Text = _localization.Lang("settings.allow_multi_press");
            _controls.LblTimes.Text = _localization.Lang("settings.times");
            _controls.LblLanguage.Text = _localization.Lang("settings.language");
            _controls.ChkShowAlert.Text = _localization.Lang("settings.show_alert");
            _controls.ChkMinimizeOnStart.Text = _localization.Lang("settings.minimize_on_start");
            _controls.ChkMinimizeToTray.Text = _localization.Lang("settings.minimize_to_tray");
            _controls.ChkLaunchOnStartup.Text = _localization.Lang("settings.launch_on_startup");
            _controls.LnkMoreTools.Text = _localization.Lang("links.more_tools");

            _trayManager?.RefreshLocalization();
            _lastLayout = IntPtr.Zero;
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
            _controls.TooltipTimer.Stop();
            var cursorPos = Cursor.Position;
            _controls.StatusToolTip.Show(status, this, this.PointToClient(cursorPos), 1000);
            _controls.TooltipTimer.Start();
        }

        private void UpdateLayoutDisplay(IntPtr layout)
        {
            if (layout == _lastLayout)
                return;

            _lastLayout = layout;

            string keyboardLayoutName = KeyboardLayoutInfo.GetLayoutName(layout);
            var culture = KeyboardLayoutInfo.GetCultureInfo(layout);

            string text;
            if (culture != null)
            {
                text = _localization.Lang("status.layout_display", new Dictionary<string, string>
                {
                    { ":layout", keyboardLayoutName },
                    { ":language", culture.DisplayName }
                });
            }
            else
            {
                text = _localization.Lang("status.layout_display_hkl", new Dictionary<string, string>
                {
                    { ":layout", keyboardLayoutName },
                    { ":hkl", $"0x{layout.ToInt64():X}" }
                });
            }

            _controls.LayoutLabel.Text = text;
            this.Text = text;
            _trayManager?.UpdateTooltip(text);

            if (_initialLayoutSet && AppSettings.Instance.ShowAlertOnLayoutChange)
            {
                LayoutAlertDialog.Show(keyboardLayoutName, _localization);
            }
            _initialLayoutSet = true;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_INPUTLANGCHANGE)
            {
                IntPtr newLayout = m.LParam;
                UpdateLayoutDisplay(newLayout);
            }
            else if (m.Msg == WM_SETTINGCHANGE)
            {
                UpdateTrayIconForTheme();
                UpdateWindowIconForTheme();
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
            try
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(DoRestoreFromTray));
                    return;
                }

                DoRestoreFromTray();
            }
            catch
            {
                // Ignore restore errors
            }
        }

        private void DoRestoreFromTray()
        {
            _isRestoring = true;
            try
            {
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
                this.Visible = true;
                this.Activate();
                this.BringToFront();
            }
            finally
            {
                _isRestoring = false;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _timer?.Stop();
            _timer?.Dispose();
            _keyboardHook?.Dispose();
            _trayManager?.Dispose();
            _controls?.TooltipTimer?.Stop();
            _controls?.TooltipTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
