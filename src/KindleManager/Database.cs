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
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly LiteDatabase db;
        public readonly string DBFile;
        public ObservableCollection<BookEntry> BOOKS { get; set; }

        /// <summary>
        /// Gets next usable ID for book entries table
        /// </summary>
        public static int NextID()
        {
            DatabaseMetadata m = App.LocalLibrary.Database.ReadMetadata();
            if (m == null)
            {
                Logger.Error("Database does not contain METADATA collection and may be corrupt");
                throw new IDNotFoundException("Metadata entry could not be found in database");
            }
            m.LastUsedID++;

            App.LocalLibrary.Database.db.GetCollection<DatabaseMetadata>("METADATA").Update(m);

            return m.LastUsedID;
        }

        public Database(string DBFile)
        {
            Logger.Info("Connecting to database {}", DBFile);
            db = new LiteDatabase(DBFile);
            BOOKS = new ObservableCollection<BookEntry>(db.GetCollection<BookEntry>("BOOKS").FindAll());
            if (!db.CollectionExists("METADATA"))
            {
                Logger.Info("Creating new METADATA collection in database");
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
            Logger.Info("Adding {} [{}] to database.", book.Title, book.Id);
            if (BOOKS.Any(x => x.Id == book.Id))
            {
                throw new LiteException($"{book.FilePath} [{book.Id}] already exists in library"); ;
            }

            BookEntry entry = new BookEntry(book);
            entry.DateAdded = DateTime.Now.ToString("M/d/yyyy");

            entry.Id = book.Id != 0 ? book.Id : NextID();

            db.GetCollection<BookEntry>("BOOKS").Insert(entry);

            // ObservableCollections *must* be updated from the main/ui thread
            App.Current.Dispatcher.Invoke(() =>
            {
                BOOKS.Add(entry);
            });
        }

        #endregion

        #region Read

        private DatabaseMetadata ReadMetadata()
        {
            return db.GetCollection<DatabaseMetadata>("METADATA").FindById(1);
        }

        public string MetadataDump(BookBase b)
        {
            return db.GetCollection("BOOKS").FindById(b.Id).ToString();
        }

        /// <summary>
        /// Finds the best match in library or null.
        /// </summary>
        public BookBase FindMatch(BookBase b)
        {
            // This can be expanded? There aren't a lot of good uuids for books
            return b.ISBN == 0 ? null : BOOKS.First(x => x.ISBN == b.ISBN);
        }

        public string[] ListAuthors()
        {
            HashSet<string> authors = new HashSet<string>();

            foreach (var book in BOOKS)
            {
                authors.Add(book.Author);
            }
            return authors.ToArray();
        }

        public string[] ListSeries()
        {
            HashSet<string> series = new HashSet<string>();

            foreach (var book in BOOKS)
            {
                series.Add(book.Series);
            }
            return series.ToArray();
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

            BookEntry tableRow = BOOKS.First(x => x.Id == update.Id);

            dbEntry = new BookEntry(update);
            tableRow.CopyFrom(update);

            col.Update(dbEntry);

            App.Current.Dispatcher.Invoke(() =>
            {
                int index = BOOKS.IndexOf(tableRow);
                BOOKS.Move(index, index);
            });
        }

        /// <summary>
        /// Changes id for entry by removing from db, updating id, then re-inserting
        /// </summary>
        public void ChangeBookId(BookEntry book, int Id)
        {
            Logger.Info("Changing {} ID from [{}] to [{}]", book.Title, book.Id, Id);
            RemoveBook(book);
            book.Id = Id;
            AddBook(book);
        }

        #endregion

        #region Delete
        public void RemoveBook(BookEntry book)
        {
            Logger.Info("Removing {} [{}] from database.", book.Title, book.Id);
            var c = db.GetCollection<BookEntry>("BOOKS");
            c.Delete(x => x.Id == book.Id);
            BOOKS.Remove(book);
        }

        public void RemoveBook(int id)
        {
            Logger.Info("Removing ID [{}] from database.", id);
            var c = db.GetCollection<BookEntry>("BOOKS");
            c.Delete(x => x.Id == id);
            BookEntry m = BOOKS.FirstOrDefault(x => x.Id == id);
            if (m != null) BOOKS.Remove(m);
        }

        /// <summary>
        /// Drops DB collection and clears associated observables
        /// </summary>
        /// <param name="collection"></param>
        public void Drop(string collection)
        {
            Logger.Info("Dropping collection {} from database.", collection);
            db.DropCollection(collection);

            if (collection == "BOOKS") BOOKS.Clear();

        }
        #endregion

        public void Dispose() => db.Dispose();

        public class BookEntry : BookBase
        {
            [Reactive] public override int Id { get; set; }

            [Reactive] public override string FilePath { get; set; } = "";

            [Reactive] public override string Title { get; set; } = "";
            [Reactive] public override string Language { get; set; } = "";
            [Reactive] public override ulong ISBN { get; set; }

            [Reactive] public override string Author { get; set; } = "";
            [Reactive] public override string Contributor { get; set; } = "";
            [Reactive] public override string Publisher { get; set; } = "";
            [Reactive] public override string[] Subject { get; set; }
            [Reactive] public override string Description { get; set; } = "";
            [Reactive] public override string PubDate { get; set; } = "";
            [Reactive] public override string Rights { get; set; } = "";

            [Reactive] public override string Series { get; set; } = "";
            [Reactive] public override Nullable<float> SeriesNum { get; set; }
            [Reactive] public override string DateAdded { get; set; } = "";

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
