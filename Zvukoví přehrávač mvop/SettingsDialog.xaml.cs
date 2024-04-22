using Newtonsoft.Json;
using System.Collections.Generic;
using System.Windows;
using static Zvukoví_přehrávač_mvop.MainWindow;

namespace Zvukoví_přehrávač_mvop
{
    public partial class SettingsDialog : Window
    {
        private const string FilePath = "folderPaths.json";
        public string PathToSongFolder { get; set; }

        public SettingsDialog()
        {
            InitializeComponent();
            DataContext = this;

                        PathToSongFolder = LoadFolderPaths()[0];
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
                        DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
                        DialogResult = false;
        }
        public static List<string> LoadFolderPaths()
        {
            if (System.IO.File.Exists(FilePath))
            {
                string json = System.IO.File.ReadAllText(FilePath);
                FolderPaths folderPaths = JsonConvert.DeserializeObject<FolderPaths>(json);
                return folderPaths.Paths;
            }
            return new List<string>();
        }
    }
}
