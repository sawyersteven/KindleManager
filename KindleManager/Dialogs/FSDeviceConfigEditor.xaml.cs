using System.Windows;

namespace KindleManager.Dialogs
{
    public partial class FSDeviceConfigEditor
    {
        public Config.FSDeviceConfig Config { get; set; }
        public bool RequestReorg = false;
        private readonly Config.FSDeviceConfig OrigConfig;
        public bool DialogResult = false;

        public FSDeviceConfigEditor(Config.FSDeviceConfig config)
        {
            OrigConfig = config;
            this.DataContext = this;
            this.Config = new Config.FSDeviceConfig(config);
            InitializeComponent();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand.Execute(this, null);
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            RequestReorg = Config.DirectoryFormat != OrigConfig.DirectoryFormat;
            DialogResult = true;
            this.Close(sender, e);
        }

    }
}
