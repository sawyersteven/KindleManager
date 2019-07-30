using System.Windows;

namespace KindleManager.Dialogs
{
    /// <summary>
    /// Interaction logic for Error.xaml
    /// </summary>
    public partial class Error
    {
        public Error(string title, string message)
        {
            this.DataContext = new ViewModels.ErrorDialog(title, message);
            InitializeComponent();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand.Execute(this, null);
        }
    }
}
