using System.Collections.Generic;
using System.Windows;

namespace ETools
{
    public partial class Panels : Window
    {
        public Panels(List<string> panelNames)
        {
            InitializeComponent();
            PopulateComboBox(panelNames);
        }

        public string selectedPanelName { get; set; }
        public bool pickFromModel { get; set; } = false;

        private void PopulateComboBox(List<string> panelNames)
        {
            comboBox.ItemsSource = panelNames;
        }

        private void PickPanel_Click(object sender, RoutedEventArgs e)
        {
            pickFromModel = true;
            this.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            selectedPanelName = comboBox.SelectedItem?.ToString() ?? string.Empty;
            this.Close();
        }
    }
}