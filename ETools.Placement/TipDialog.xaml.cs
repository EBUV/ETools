using System.Windows;

namespace ETools.Placement
{
    public partial class TipDialog : Window
    {
        private readonly string _settingsKey;

        public TipDialog(string message, string settingsKey)
        {
            InitializeComponent();
            InstructionText.Text = message;
            _settingsKey = settingsKey;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (DoNotShowAgainCheckBox.IsChecked == true)
            {
                SettingsManager.SetBool(_settingsKey, false);
            }
            this.Close();
        }
    }
}
