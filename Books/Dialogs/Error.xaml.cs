using System.Windows;
using MahApps.Metro.Controls;

namespace Books.Dialogs
{
    /// <summary>
    /// Interaction logic for Error.xaml
    /// </summary>
    public partial class Error : MetroWindow
    {
        public Error(string title, string message)
        {
            this.DataContext = new ViewModels.ErrorDialog(title, message);
            this.Owner = App.Current.MainWindow;
            InitializeComponent();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
