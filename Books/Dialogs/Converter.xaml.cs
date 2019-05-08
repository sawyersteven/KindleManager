using System.Windows;
using Formats;
using System.IO;

namespace Books.Dialogs
{
    public partial class Converter : Window
    {
        IBook InputBook { get; }
        string OutputPath { get; set; }

        public Converter(IBook book)
        {
            this.DataContext = this;
            InputBook = book;
            this.Owner = App.Current.MainWindow;
            InitializeComponent();

            OutputPath = Path.ChangeExtension(InputBook.FilePath, "mobi");


        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void StartConversion(object sender, RoutedEventArgs e)
        {


        }

    }
}
