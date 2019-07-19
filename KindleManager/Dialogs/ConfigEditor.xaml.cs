using KindleManager.Config;
using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Forms;

namespace KindleManager.Dialogs
{
    public partial class ConfigEditor : MetroWindow
    {
        public PCConfig Config { get; }
        public bool HelpOpen { get; set; }

        private void ToggleHelpOpen(object sender, RoutedEventArgs e)
        {
            HelpOpen = !HelpOpen;
        }

        public ConfigEditor()
        {
            this.DataContext = this;
            this.Config = new PCConfig(App.Config);
            this.Owner = App.Current.MainWindow;
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
            App.Config = Config;
            try
            {
                App.Config.Write();
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }

            DialogResult = true;
            this.Close();
        }

    }
}
