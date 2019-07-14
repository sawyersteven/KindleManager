using MahApps.Metro.Controls;
using System.Windows;

namespace KindleManager.Dialogs
{
    public partial class DeviceConfigEditor : MetroWindow
    {
        public Devices.DeviceConfig Config { get; set; }
        private readonly Devices.Device Kindle;
        public bool HelpOpen { get; set; }

        private void ToggleHelpOpen(object sender, RoutedEventArgs e)
        {
            HelpOpen = !HelpOpen;
        }

        public DeviceConfigEditor(Devices.DeviceConfig config)
        {
            this.DataContext = this;
            this.Config = new Devices.DeviceConfig(config);
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
            DialogResult = true;
            this.Close();
        }

    }
}
