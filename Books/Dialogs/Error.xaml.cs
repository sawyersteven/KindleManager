using System.Windows;

namespace Books.Dialogs
{
    /// <summary>
    /// Interaction logic for Error.xaml
    /// </summary>
    public partial class Error : Window
    {
        public Error(string title, string message)
        {
            this.DataContext = new ViewModels.ErrorDialog(title, message);
            InitializeComponent();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
