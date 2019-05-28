using System.Windows;
using MahApps.Metro.Controls;
using System.Collections.Generic;
using ExtensionMethods;

namespace Books.Dialogs
{
    public partial class MetadataEditor : MetroWindow
    {
        #region Properties
        public string[] AuthorsList { get; set; }
        public string[] SeriesList { get; set; }
        public string[] PublisherList { get; set; }
        public Formats.BookBase Book { get; set; }
        public Formats.BookBase ModBook { get; set; }
        #endregion

    public MetadataEditor(Database.BookEntry book)
        {
            this.Owner = App.Current.MainWindow;
            this.DataContext = this;
            this.Title = book.Title;
            this.Book = book;
            this.AuthorsList = App.Database.ListAuthors();
            this.SeriesList = App.Database.ListSeries();

            HashSet<string> _authors = new HashSet<string>();
            HashSet<string> _series = new HashSet<string>();
            HashSet<string> _publishers = new HashSet<string>();

            foreach (var b in App.Database.Library)
            {
                _authors.Add(b.Author);
                _series.Add(b.Series);
                _publishers.Add(b.Publisher);
            }
            AuthorsList = _authors.ToArray();
            SeriesList = _series.ToArray();
            PublisherList = _publishers.ToArray();

            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this.MinWidth = this.ActualWidth;
            this.MinHeight = this.ActualHeight;
            this.MaxHeight = this.ActualHeight;
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void CloseAndSave(object sender, RoutedEventArgs e)
        {
            ModBook = new Database.BookEntry(Book);
            ModBook.PubDate = Utils.Metadata.GetDate(ModBook.PubDate);
            DialogResult = true;
            this.Close();
        }
    }
}
