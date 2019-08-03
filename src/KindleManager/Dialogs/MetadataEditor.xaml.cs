using ExtensionMethods;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;

namespace KindleManager.Dialogs
{
    public partial class MetadataEditor : DialogBase
    {
        #region Properties
        public string[] AuthorsList { get; set; }
        public string[] SeriesList { get; set; }
        public string[] PublisherList { get; set; }
        public Formats.BookBase ModBook { get; set; }
        #endregion

        private static readonly Regex onlyFloat = new Regex("[^0-9.-]+");

        public MetadataEditor(Database.BookEntry book, HashSet<string> authors, HashSet<string> series, HashSet<string> publishers)
        {
            this.DataContext = this;
            this.AuthorsList = App.LocalLibrary.Database.ListAuthors();
            this.SeriesList = App.LocalLibrary.Database.ListSeries();
            this.ModBook = new Database.BookEntry(book);

            AuthorsList = authors.ToArray();
            SeriesList = series.ToArray();
            PublisherList = publishers.ToArray();

            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this.MinWidth = this.ActualWidth;
            this.MinHeight = this.ActualHeight;
            this.MaxHeight = this.ActualHeight;
        }

        private void CloseAndSave(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close(sender, e);
        }

        private void TextBoxFloatOnly(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = onlyFloat.IsMatch(e.Text);
        }

        private void TextBoxPasteFloatOnly(object sender, DataObjectPastingEventArgs e)
        {
            string input = (string)e.DataObject.GetData(typeof(string));
            if (onlyFloat.IsMatch(input)) e.CancelCommand();
        }

        private void FloatBoxLostFocus(object sender, RoutedEventArgs e)
        {
            var tb = (System.Windows.Controls.TextBox)sender;
            if (tb.Text == "") tb.Text = null;
            if (float.TryParse(tb.Text, out float f))
            {
                tb.Text = f.ToString("F1");
            }
            else
            {
                tb.Text = "";
            }
        }
    }
}
