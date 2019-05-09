using System.Windows;

namespace Books.Dialogs
{
    public partial class ConfigEditor : Window
    {
        public ConfigManager.Config Config { get; }

        public ConfigEditor()
        {
            this.DataContext = this;
            this.Config = new ConfigManager.Config(App.ConfigManager.config);
            this.Owner = App.Current.MainWindow;
            InitializeComponent();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            App.ConfigManager.config = Config;
            App.ConfigManager.Write();

            DialogResult = true;
            this.Close();
        }

    }
}
