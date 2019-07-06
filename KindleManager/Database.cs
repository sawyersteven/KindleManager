using ExtensionMethods;
using Formats;
using LiteDB;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KindleManager
{
    public class Database : IDisposable
    {
        private LiteDatabase db;
        public readonly string DBFile;
        public ObservableCollection<BookEntry> Library { get; set; }

        public Database(string DBFile)
        {
            db = new LiteDatabase(DBFile);
            Library = new ObservableCollection<BookEntry>(db.GetCollection<BookEntry>("BOOKS").FindAll());
            if (!db.CollectionExists("METADATA"))
            {
                DatabaseMetadata m = new DatabaseMetadata();
                m.Id = 1;
                m.Version = 1;
                m.LastUsedID = 0;
                db.GetCollection<DatabaseMetadata>("METADATA").Insert(m);
            }
        }

        #region Create

        public void AddBook(BookBase book)
        {
            if (Library.Any(x => x.Id == book.Id))
            {
                throw new LiteException($"{book.FilePath} [{book.Id}] already exists in library"); ;
            }

            BookEntry entry = new BookEntry(book);
            entry.DateAdded = DateTime.Now.ToString("yyyy-MM-dd"); // 1950-01-01

            if (book.Id != 0)
            {
                entry.Id = book.Id;
            }

            db.GetCollection<BookEntry>("BOOKS").Insert(entry);

            // ObservableCollections *must* be updated from the main/ui thread
            App.Current.Dispatcher.Invoke(() =>
            {
                Library.Add(entry);
            });

        }

        #endregion

        #region Read

        public int NextID()
        {
            return db.GetCollection("BOOKS").Max("_id").AsInt32 + 1;
        }

        /// <summary>
        /// Finds the best match in library or null.
        /// </summary>
        public BookBase FindMatch(BookBase b)
        {
            // This can be expanded? There aren't a lot of good uuids for books
            return b.ISBN == 0 ? null : Library.First(x => x.ISBN == b.ISBN);
        }

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

        public BookEntry GetByFileName(string FileName)
        {
            return Library.FirstOrDefault(x => x.FilePath == FileName);
        }

        public BookEntry GetById(int id)
        {
            return Library.FirstOrDefault(x => x.Id == id);
        }

        #endregion

        #region Update
        /// <summary>
        /// Updates BOOKS entry with matching Id
        /// Raises exception if filename not in colletion
        /// </summary>
        public void UpdateBook(BookBase update)
        {
            var col = db.GetCollection<BookEntry>("BOOKS");
            BookEntry dbEntry = col.FindOne(x => x.Id == update.Id);
            if (dbEntry == null)
            {
                throw new IDNotFoundException($"{update.FilePath} not found in library");
            }

            BookEntry tableRow = Library.First(x => x.Id == update.Id);

            dbEntry = new BookEntry(update);
            tableRow.CopyFrom(update);

            col.Update(dbEntry);
        }
        #endregion

        #region Delete
        public void RemoveBook(BookEntry book)
        {
            var c = db.GetCollection<BookEntry>("BOOKS");
            c.Delete(x => x.FilePath == book.FilePath);
            Library.Remove(book);

        }

        /// <summary>
        /// Drops DB tables.
        /// Does not make a backup.
        /// Be careful.
        /// </summary>
        public void ScorchedEarth()
        {
            // Manually list all database tables because it throws weird errors
            //  I can't figure out when calling getcollectionnames()
            db.DropCollection("BOOKS");
            Library.Clear();
        }
        #endregion

        public void Dispose() => db.Dispose();

        public class BookEntry : BookBase
        {
            [Reactive] public override int Id { get; set; }

            [Reactive] public override string FilePath { get; set; }

            [Reactive] public override string Title { get; set; }
            [Reactive] public override string Language { get; set; }
            [Reactive] public override ulong ISBN { get; set; }

            [Reactive] public override string Author { get; set; }
            [Reactive] public override string Contributor { get; set; }
            [Reactive] public override string Publisher { get; set; }
            [Reactive] public override string[] Subject { get; set; }
            [Reactive] public override string Description { get; set; }
            [Reactive] public override string PubDate { get; set; }
            [Reactive] public override string Rights { get; set; }

            [Reactive] public override string Series { get; set; }
            [Reactive] public override float SeriesNum { get; set; }
            [Reactive] public override string DateAdded { get; set; }

            #region methods
            public override string TextContent() => "";
            public override byte[][] Images() => new byte[0][];
            public override void WriteMetadata() { }
            #endregion

            public BookEntry() { }

            public BookEntry(BookBase b)
            {
                this.CopyFrom(b);
            }

            public void CopyFrom(BookBase b)
            {
                this.FilePath = b.FilePath;
                this.Title = b.Title;
                this.Language = b.Language;
                this.ISBN = b.ISBN;
                this.Author = b.Author;
                this.Contributor = b.Contributor;
                this.Publisher = b.Publisher;
                this.Subject = b.Subject;
                this.Description = b.Description;
                this.PubDate = b.PubDate;
                this.Rights = b.Rights;
                this.Id = b.Id;
                this.Series = b.Series;
                this.SeriesNum = b.SeriesNum;
                this.DateAdded = b.DateAdded;
            }
        }

        public class DatabaseMetadata
        {
            public int Id { get; set; }
            public int Version { get; set; }
            public int LastUsedID { get; set; }
        }

        public class IDNotFoundException : Exception
        {
            public IDNotFoundException() { }

            public IDNotFoundException(string message)
            : base(message)
            { }

            public IDNotFoundException(int id)
                    : base($"ID {id} not found in database")
            { }
        }
    }
}
