using System.Data.SQLite;
using System.IO;
using LiteDB;
using Formats;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Books
{
    public class Database : IDisposable
    {
        private LiteDatabase db;
        public readonly ObservableCollection<BookEntry> Library;

        public Database(string DBFile)
        {
            db = new LiteDatabase(DBFile);
            Library = new ObservableCollection<BookEntry>(db.GetCollection<BookEntry>("BOOKS").FindAll());
        }


        #region Create

        public void AddBook(IBook book)
        {

            if (Library.Any(x => x.FilePath == book.FilePath))
            {
                throw new InvalidOperationException($"{book.FilePath} already exists in library"); ;
            }

            var c = db.GetCollection<BookEntry>("BOOKS");

            BookEntry entry = new BookEntry() { Format = book.Format };
            entry.Title = book.Title;
            entry.FilePath = book.FilePath;
            entry.Format = book.Format;
            entry.Author = book.Author;
            entry.Series = book.Series;
            entry.SeriesNum = book.SeriesNum;
            entry.Publisher = book.Publisher;
            entry.PubDate = book.PubDate;
            entry.ISBN = book.ISBN;
            entry.DateAdded = DateTime.Now.ToString("yyyy-MM-dd"); // 1950-01-01

            c.Insert(entry);
            Library.Add(entry);
        }

        #endregion

        #region Read

        public string[] ListAuthors()
        {
            HashSet<string> authors = new HashSet<string>();

            foreach (var book in Library)
            {
                authors.Add(book.Author);
            }
            return authors.ToArray();
        }

        public string[] ListSeries()
        {
            HashSet<string> series = new HashSet<string>();

            foreach (var book in Library)
            {
                series.Add(book.Series);
            }
            return series.ToArray();
        }

        #endregion

        #region Update
        /// <summary>
        /// Updates BOOKS entry with matching filename
        /// Raises exception if filename not in colletion
        /// </summary>
        public void UpdateBook(IBook update)
        {
            var col = db.GetCollection<BookEntry>("BOOKS");
            BookEntry dbEntry = col.FindOne(x => x.Id == update.Id);
            if (dbEntry == null)
            {
                throw new Exception($"{update.FilePath} not found in library");
            }

            BookEntry tableRow = Library.First(x => x.Id == update.Id);
            Library.Remove(tableRow);

            var updateProps = update.GetType().GetProperties();

            foreach (var prop in updateProps)
            {
                dbEntry.GetType().GetProperty(prop.Name).SetValue(dbEntry, prop.GetValue(update));
                tableRow.GetType().GetProperty(prop.Name).SetValue(tableRow, prop.GetValue(update));
            }

            col.Update(dbEntry);
            Library.Add(tableRow);
        }
        #endregion

        #region Delete
        public void RemoveBook(BookEntry book)
        {
            var c = db.GetCollection<BookEntry>("BOOKS");
            c.Delete(x => x.FilePath == book.FilePath);
            Library.Remove(book);

        }
        #endregion

        public void Dispose()
        {
            db.Dispose();
        }



        public class BookEntry : IBook
        {
            public string FilePath { get; set; }
            public string Format { get; set; }

            public string Title { get; set; }
            public string Language { get; set; }
            public ulong ISBN { get; set; }

            public string Author { get; set; }
            public string Contributor { get; set; }
            public string Publisher { get; set; }
            public string[] Subject { get; set; }
            public string Description { get; set; }
            public string PubDate { get; set; }
            public string Rights { get; set; }

            public int Id { get; set; }
            public string Series { get; set; }
            public float SeriesNum { get; set; }
            public string DateAdded { get; set; }

            #region methods
            public string TextContent() => "";
            public void WriteMetadata() { }
            public void WriteContent(string text, byte[][] images) { }
            #endregion
        }
    }
}
