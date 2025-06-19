using System.Windows;

namespace ETools.Placement
{
    public partial class ArrayInputDialog : Window
    {
        public int Columns { get; private set; } = 1;
        public int Rows { get; private set; } = 1;

        private const int MaxElements = 1000;

        public ArrayInputDialog()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(ColumnsTextBox.Text, out int cols) || cols <= 0)
            {
                MessageBox.Show("Please enter a valid positive number for columns.",
                                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(RowsTextBox.Text, out int rows) || rows <= 0)
            {
                MessageBox.Show("Please enter a valid positive number for rows.",
                                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int total = cols * rows;
            if (total > MaxElements)
            {
                MessageBox.Show($"You are trying to place {total} elements.\n" +
                                $"The maximum allowed is {MaxElements}.\n" +
                                $"Please reduce the number of rows or columns.",
                                "Too Many Elements", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Columns = cols;
            Rows = rows;

            this.DialogResult = true;
            this.Close();
        }
    }
}
