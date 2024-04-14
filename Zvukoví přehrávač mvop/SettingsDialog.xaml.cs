using System.Windows;

namespace Zvukoví_přehrávač_mvop
{
    public partial class SettingsDialog : Window
    {
        // Property to bind the TextBox text to
        public string PathToSongFolder { get; set; }

        public SettingsDialog()
        {
            InitializeComponent();
            DataContext = this; // Set DataContext to this window for data binding
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Implement logic to open a folder browser dialog and set PathToSongFolder
            // Example:
            // var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            // if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            // {
            //     PathToSongFolder = folderBrowserDialog.SelectedPath;
            // }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            // Implement logic to save the changes and close the dialog
            // Example:
            // DialogResult = true;
            // Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Implement logic to discard the changes and close the dialog
            // Example:
            // DialogResult = false;
            // Close();
        }
    }
}
