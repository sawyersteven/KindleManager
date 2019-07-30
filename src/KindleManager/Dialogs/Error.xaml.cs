using System.Windows;

namespace KindleManager.Dialogs
{
    /// <summary>
    /// Interaction logic for Error.xaml
    /// </summary>
    public partial class Error
    {
        public string TitleText { get; set; }
        public string MessageText { get; set; }

        public Error(string title, string message)
        {
            TitleText = title;
            MessageText = message;
            this.DataContext = this;
            InitializeComponent();
        }
        private void Close(object sender, RoutedEventArgs e)
        {
            MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand.Execute(this, null);
        }
    }
}
