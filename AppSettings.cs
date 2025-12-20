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
        public bool BlockWinSpace { get; set; }

        public void Load()
        {
            ShowAlertOnLayoutChange = Properties.Settings.Default.ShowAlertOnLayoutChange;
            MinimizeOnStart = Properties.Settings.Default.MinimizeOnStart;
            MinimizeToTray = Properties.Settings.Default.MinimizeToTray;
            BlockWinSpace = Properties.Settings.Default.BlockWinSpace;
        }

        public void Save()
        {
            Properties.Settings.Default.ShowAlertOnLayoutChange = ShowAlertOnLayoutChange;
            Properties.Settings.Default.MinimizeOnStart = MinimizeOnStart;
            Properties.Settings.Default.MinimizeToTray = MinimizeToTray;
            Properties.Settings.Default.BlockWinSpace = BlockWinSpace;
            Properties.Settings.Default.Save();

            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
