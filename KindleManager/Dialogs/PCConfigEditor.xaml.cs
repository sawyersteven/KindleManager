using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Forms;

namespace KindleManager.Dialogs
{
    public partial class PCConfigEditor : MetroWindow
    {
        public ConfigManager<Config.PCConfig> ConfigManager { get; set; }
        public Config.PCConfig Config { get; }
        public bool HelpOpen { get; set; }

        private void ToggleHelpOpen(object sender, RoutedEventArgs e)
        {
            HelpOpen = !HelpOpen;
        }

        public PCConfigEditor(ConfigManager<Config.PCConfig> confManager)
        {
            ConfigManager = confManager;
            DataContext = this;
            Config = confManager.Copy();
            Owner = App.Current.MainWindow;
            InitializeComponent();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void SelectLibraryDir(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            dlg.ShowDialog();
            if (dlg.SelectedPath != null)
            {
                Config.LibraryRoot = dlg.SelectedPath;
            }
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            ConfigManager.Write(Config);

            DialogResult = true;
            this.Close();
        }

    }
}
