using System.Windows;

namespace ETools.Placement
{
    public partial class ArrayInputDialog : Window
    {
        public int Columns { get; private set; } = 1;
        public int Rows { get; private set; } = 1;

        public ArrayInputDialog()
        {
            InitializeComponent();

            // Hide the checkbox if the user already disabled it
            if (!SettingsManager.GetBool("ShowTip_ArrayPlace"))
            {
                DoNotShowAgainCheckBox.Visibility = Visibility.Collapsed;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(ColumnsTextBox.Text, out int cols) || cols <= 0)
            {
                MessageBox.Show("Please enter a valid number of columns.");
                return;
            }

            if (!int.TryParse(RowsTextBox.Text, out int rows) || rows <= 0)
            {
                MessageBox.Show("Please enter a valid number of rows.");
                return;
            }

            Columns = cols;
            Rows = rows;

            if (DoNotShowAgainCheckBox.IsChecked == true)
            {
                SettingsManager.SetBool("ShowTip_ArrayPlace", false);
            }

            this.DialogResult = true;
        }
    }
}
