using ReactiveUI;
using System.Reactive;
using System.Collections.Generic;
using System;
using ExtensionMethods;

namespace Books.ViewModels
{
    class MetadataEditor : ReactiveObject
    {

        public MetadataEditor(Formats.IBook book)
        {
            this.Title = book.Title;

            SaveMetadata = ReactiveCommand.Create(_SaveMetadata);

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

        }

        #region Properties
        public string[] AuthorsList { get; set; }
        public string[] SeriesList { get; set; }
        public string[] PublisherList { get; set; }
        public Formats.IBook Book { get; set; }
        public string Title { get; set; }
        public Action CloseDialog { get; set; }
        #endregion

        #region Button Functions
        public ReactiveCommand<Unit, Unit> SaveMetadata { get; set; }
        private void _SaveMetadata()
        {
            try
            {
                this.Book.WriteMetadata();
                App.Database.UpdateBook(Book);
                CloseDialog();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                var err = new Dialogs.Error("Unable to save metadata", e.Message);
                err.ShowDialog();
            }

        }
        #endregion
    }
}