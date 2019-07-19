using MahApps.Metro.Controls;
using System.Windows;

namespace KindleManager.Dialogs
{
    public partial class FSDeviceConfigEditor : MetroWindow
    {
        public Config.FSDeviceConfig Config { get; set; }
        public bool HelpOpen { get; set; }
        public bool RequestReorg = false;
        private readonly Config.FSDeviceConfig OrigConfig;

        private void ToggleHelpOpen(object sender, RoutedEventArgs e)
        {
            HelpOpen = !HelpOpen;
        }

        public FSDeviceConfigEditor(Config.FSDeviceConfig config)
        {
            OrigConfig = config;
            this.DataContext = this;
            this.Config = new Config.FSDeviceConfig(config);
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
            RequestReorg = Config.DirectoryFormat != OrigConfig.DirectoryFormat;
            DialogResult = true;
            this.Close();
        }

    }
}
