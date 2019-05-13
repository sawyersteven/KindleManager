using System.Windows;
using MahApps.Metro.Controls;
using System.Windows.Controls;

namespace Books.Dialogs
{
    public partial class DeviceConfigEditor : MetroWindow
    {
        public Devices.DeviceConfig Config { get; set; }
        private readonly Devices.IDevice Kindle;
        public bool HelpOpen { get; set; }

        private void ToggleHelpOpen(object sender, RoutedEventArgs e)
        {
            HelpOpen = !HelpOpen;
        }

        public DeviceConfigEditor(Devices.IDevice kindle)
        {
            this.DataContext = this;
            this.Kindle = kindle;
            this.Config = new Devices.DeviceConfig(kindle.Config);
            this.Owner = App.Current.MainWindow;
            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));
            InitializeComponent();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            Kindle.WriteConfig(Config);
            DialogResult = true;
            this.Close();
        }

    }
}
