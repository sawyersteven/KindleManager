using MahApps.Metro.Controls;
using System.Windows;

namespace KindleManager.Dialogs
{
    public partial class DeviceConfigEditor : MetroWindow
    {
        public ConfigManager<Config.DeviceConfig> ConfigManager { get; set; }
        public Config.DeviceConfig Config { get; set; }
        public bool HelpOpen { get; set; }

        private void ToggleHelpOpen(object sender, RoutedEventArgs e)
        {
            HelpOpen = !HelpOpen;
        }

        public DeviceConfigEditor(ConfigManager<Config.DeviceConfig> confManager)
        {
            ConfigManager = confManager;
            DataContext = this;
            Config = ConfigManager.Copy();
            Owner = App.Current.MainWindow;
            InitializeComponent();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

    }
}
