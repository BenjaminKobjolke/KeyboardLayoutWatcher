using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CSharpLocalization;

namespace KeyboardLayoutWatcher
{
    public static class LayoutAlertDialog
    {
        private static Form _currentAlert = null;

        public static void Show(string layoutName, Localization localization)
        {
            if (_currentAlert != null && !_currentAlert.IsDisposed)
            {
                _currentAlert.Close();
                _currentAlert.Dispose();
            }

            _currentAlert = new Form
            {
                Text = localization.Lang("alert.title"),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                Size = new Size(300, 150),
                MaximizeBox = false,
                MinimizeBox = false,
                TopMost = true,
                ShowInTaskbar = false,
                BackColor = ThemeColors.DarkBackground
            };

            var alertMessage = localization.Lang("alert.message", new Dictionary<string, string>
            {
                { ":layout", layoutName }
            });

            var label = new Label
            {
                Text = alertMessage,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = ThemeFonts.Heading,
                ForeColor = ThemeColors.TextPrimary,
                BackColor = ThemeColors.DarkBackground
            };
            _currentAlert.Controls.Add(label);

            var okButton = new Button
            {
                Text = localization.Lang("alert.ok"),
                Size = new Size(75, 30),
                Location = new Point(112, 80),
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeColors.Separator,
                ForeColor = ThemeColors.TextPrimary
            };
            okButton.FlatAppearance.BorderColor = ThemeColors.Border;
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
    }
}
