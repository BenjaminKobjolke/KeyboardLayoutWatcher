using System.Drawing;
using System.Windows.Forms;
using CSharpLocalization;
using KeyboardLayoutWatcher.Config;

namespace KeyboardLayoutWatcher
{
    public class MainFormControls
    {
        public Label LayoutLabel { get; set; }
        public RadioButton RbBlockCompletely { get; set; }
        public RadioButton RbAllowMultiPress { get; set; }
        public NumericUpDown NudPressCount { get; set; }
        public Label LblTimes { get; set; }
        public CheckBox ChkShowAlert { get; set; }
        public CheckBox ChkMinimizeOnStart { get; set; }
        public CheckBox ChkMinimizeToTray { get; set; }
        public CheckBox ChkLaunchOnStartup { get; set; }
        public Label LblLanguage { get; set; }
        public ComboBox CmbLanguage { get; set; }
        public LinkLabel LnkMoreTools { get; set; }
        public ToolTip StatusToolTip { get; set; }
        public Timer TooltipTimer { get; set; }
    }

    public static class MainFormUI
    {
        public static MainFormControls CreateControls(Form form, Localization localization)
        {
            var controls = new MainFormControls();

            // Layout display label
            controls.LayoutLabel = new Label
            {
                Location = new Point(15, 10),
                Size = new Size(370, 30),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = ThemeFonts.Heading,
                ForeColor = ThemeColors.TextPrimary,
                BackColor = ThemeColors.DarkBackground
            };
            form.Controls.Add(controls.LayoutLabel);

            // Separator
            var separator = new Label
            {
                Location = new Point(10, 45),
                Size = new Size(370, 1),
                BackColor = ThemeColors.Separator
            };
            form.Controls.Add(separator);

            // Win+Space options - Radio buttons
            int yPos = 55;
            int spacing = 28;

            controls.RbBlockCompletely = new RadioButton
            {
                Text = localization.Lang("settings.block_completely"),
                Location = new Point(15, yPos),
                Size = new Size(360, 24),
                Font = ThemeFonts.Normal,
                ForeColor = ThemeColors.TextPrimary,
                BackColor = ThemeColors.DarkBackground,
                FlatStyle = FlatStyle.Flat
            };
            form.Controls.Add(controls.RbBlockCompletely);
            yPos += spacing;

            controls.RbAllowMultiPress = new RadioButton
            {
                Text = localization.Lang("settings.allow_multi_press"),
                Location = new Point(15, yPos),
                Size = new Size(230, 24),
                Font = ThemeFonts.Normal,
                ForeColor = ThemeColors.TextPrimary,
                BackColor = ThemeColors.DarkBackground,
                FlatStyle = FlatStyle.Flat
            };
            form.Controls.Add(controls.RbAllowMultiPress);

            controls.NudPressCount = new NumericUpDown
            {
                Location = new Point(250, yPos),
                Size = new Size(45, 24),
                Minimum = 2,
                Maximum = 5,
                Value = 3,
                Font = ThemeFonts.Normal,
                BackColor = ThemeColors.MenuBackground,
                ForeColor = ThemeColors.TextPrimary
            };
            form.Controls.Add(controls.NudPressCount);

            controls.LblTimes = new Label
            {
                Text = localization.Lang("settings.times"),
                Location = new Point(300, yPos + 2),
                Size = new Size(50, 24),
                Font = ThemeFonts.Normal,
                ForeColor = ThemeColors.TextPrimary,
                BackColor = ThemeColors.DarkBackground
            };
            form.Controls.Add(controls.LblTimes);
            yPos += spacing;

            // Checkboxes
            controls.ChkShowAlert = CreateCheckBox(form, localization.Lang("settings.show_alert"), yPos);
            yPos += spacing;

            controls.ChkMinimizeOnStart = CreateCheckBox(form, localization.Lang("settings.minimize_on_start"), yPos);
            yPos += spacing;

            controls.ChkMinimizeToTray = CreateCheckBox(form, localization.Lang("settings.minimize_to_tray"), yPos);
            yPos += spacing;

            controls.ChkLaunchOnStartup = CreateCheckBox(form, localization.Lang("settings.launch_on_startup"), yPos);
            yPos += spacing;

            // Language selection
            controls.LblLanguage = new Label
            {
                Text = localization.Lang("settings.language"),
                Location = new Point(15, yPos + 2),
                Size = new Size(80, 24),
                Font = ThemeFonts.Normal,
                ForeColor = ThemeColors.TextPrimary,
                BackColor = ThemeColors.DarkBackground
            };
            form.Controls.Add(controls.LblLanguage);

            controls.CmbLanguage = new ComboBox
            {
                Location = new Point(100, yPos),
                Size = new Size(150, 24),
                Font = ThemeFonts.Normal,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ThemeColors.MenuBackground,
                ForeColor = ThemeColors.TextPrimary,
                FlatStyle = FlatStyle.Flat
            };

            // Populate language dropdown
            var languages = localization.GetAvailableLanguages();
            controls.CmbLanguage.DisplayMember = "Name";
            controls.CmbLanguage.ValueMember = "Code";
            controls.CmbLanguage.DataSource = languages;

            form.Controls.Add(controls.CmbLanguage);
            yPos += spacing + 10;

            // More tools link
            controls.LnkMoreTools = new LinkLabel
            {
                Text = localization.Lang("links.more_tools"),
                Location = new Point(15, yPos),
                Size = new Size(370, 24),
                Font = ThemeFonts.SmallUnderlined,
                LinkColor = ThemeColors.TextPrimary,
                ActiveLinkColor = ThemeColors.LinkActive,
                VisitedLinkColor = ThemeColors.TextPrimary,
                BackColor = ThemeColors.DarkBackground,
                LinkBehavior = LinkBehavior.AlwaysUnderline
            };
            controls.LnkMoreTools.LinkClicked += (s, args) => System.Diagnostics.Process.Start(Constants.MoreToolsUrl);
            form.Controls.Add(controls.LnkMoreTools);

            // Status tooltip
            controls.StatusToolTip = new ToolTip
            {
                AutoPopDelay = 1000,
                InitialDelay = 0,
                ReshowDelay = 0,
                ShowAlways = true
            };

            controls.TooltipTimer = new Timer { Interval = 1000 };

            return controls;
        }

        private static CheckBox CreateCheckBox(Form form, string text, int yPos)
        {
            var checkBox = new CheckBox
            {
                Text = text,
                Location = new Point(15, yPos),
                Size = new Size(360, 24),
                Font = ThemeFonts.Normal,
                ForeColor = ThemeColors.TextPrimary,
                BackColor = ThemeColors.DarkBackground,
                FlatStyle = FlatStyle.Flat
            };
            form.Controls.Add(checkBox);
            return checkBox;
        }
    }
}
