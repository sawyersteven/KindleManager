using KindleManager.Config;
using System.Windows;
using System.Windows.Forms;

namespace KindleManager.Dialogs
{
    public partial class ConfigEditor : DialogBase
    {
        public PCConfig Config { get; }

        public ConfigEditor()
        {
            this.DataContext = this;
            this.Config = new PCConfig(App.Config);
            InitializeComponent();
        }

        private void SelectLibraryDir(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            dlg.SelectedPath = Config.LibraryRoot;
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
            this.Close(sender, e);
        }

    }
}
