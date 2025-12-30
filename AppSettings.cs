using System;

namespace KeyboardLayoutWatcher
{
    public class AppSettings
    {
        private static readonly Lazy<AppSettings> _instance = new Lazy<AppSettings>(() => new AppSettings());
        public static AppSettings Instance => _instance.Value;

        public event EventHandler SettingsChanged;

        private AppSettings()
        {
            Load();
        }

        public bool ShowAlertOnLayoutChange { get; set; }
        public bool MinimizeOnStart { get; set; }
        public bool MinimizeToTray { get; set; }
        public bool WinSpaceBlockCompletely { get; set; }  // true = block completely, false = multi-press
        public int WinSpacePressCount { get; set; }        // 2-5
        public bool LaunchOnStartup { get; set; }

        public void Load()
        {
            ShowAlertOnLayoutChange = Properties.Settings.Default.ShowAlertOnLayoutChange;
            MinimizeOnStart = Properties.Settings.Default.MinimizeOnStart;
            MinimizeToTray = Properties.Settings.Default.MinimizeToTray;
            WinSpaceBlockCompletely = Properties.Settings.Default.WinSpaceBlockCompletely;
            WinSpacePressCount = Properties.Settings.Default.WinSpacePressCount;
            LaunchOnStartup = Properties.Settings.Default.LaunchOnStartup;
        }

        public void Save()
        {
            Properties.Settings.Default.ShowAlertOnLayoutChange = ShowAlertOnLayoutChange;
            Properties.Settings.Default.MinimizeOnStart = MinimizeOnStart;
            Properties.Settings.Default.MinimizeToTray = MinimizeToTray;
            Properties.Settings.Default.WinSpaceBlockCompletely = WinSpaceBlockCompletely;
            Properties.Settings.Default.WinSpacePressCount = WinSpacePressCount;
            Properties.Settings.Default.LaunchOnStartup = LaunchOnStartup;
            Properties.Settings.Default.Save();

            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
