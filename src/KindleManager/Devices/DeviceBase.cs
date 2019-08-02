using ExtensionMethods;
using Formats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KindleManager.Devices
{
    public abstract class DeviceBase
    {
        protected NLog.Logger Logger { get; } = NLog.LogManager.GetCurrentClassLogger();

        public virtual Database Database { get; set; }

        #region abstract props
        public abstract string[] CompatibleFiletypes { get; }
        public abstract string Name { get; }
        public abstract string Description { get; set; }
        public abstract string Id { get; }
        public abstract string LibraryRoot { get; }
        #endregion

        #region abstract methods
        public abstract string AbsoluteFilePath(BookBase book);
        public abstract string RelativeFilepath(string absPath);
        public abstract string FilePathTemplate();
        public abstract bool Open();
        public abstract void Clean();

        #endregion

        #region virtual methods
        public virtual void UpdateBookMetadata(BookBase donor)
        {
            Database.BookEntry entry = Database.BOOKS.FirstOrDefault(x => x.Id == donor.Id);
            if (entry == null) return;
            BookBase recip = BookBase.Auto(AbsoluteFilePath(entry));
            recip.UpdateMetadata(donor);

            Database.UpdateBook(donor);

            string origPath = recip.FilePath;
            string targetPath = FormatFilePath(FilePathTemplate(), recip);
            if (origPath != targetPath)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                    File.Move(origPath, targetPath);
                    donor.FilePath = targetPath;
                    Database.UpdateBook(donor);
                }
                catch (Exception e)
                {
                    throw new Exception($"Could not rename file in library directory. {e.Message}");
                }
            }
        }
        #endregion

        public string FormatFilePath(string template, BookBase book)
        {
            string p = Utils.Files.MakeFilesystemSafe(template.DictFormat(book.Props()) + Path.GetExtension(book.FilePath));

            string trimmable = RelativeFilepath(p);
            string o = LibraryRoot;

            foreach (string part in trimmable.Split(Path.DirectorySeparatorChar))
            {
                o = Path.Combine(o, part.Trim());
            }
            return Path.GetFullPath(o);
        }


        /// <summary>
        /// Reorganizes library directory structure based on config
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<BookBase> Reorganize()
        {
            List<Exception> errs = new List<Exception>();

            foreach (BookBase book in Database.BOOKS.ToArray())
            {
                string origPath = AbsoluteFilePath(book);
                string dest = FormatFilePath(FilePathTemplate(), book);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    if (dest != origPath)
                    {
                        File.Move(origPath, dest);
                        Database.UpdateBook(book);
                    }
                }
                catch (Database.IDNotFoundException) { } // Don't care;
                catch (Exception e)
                {
                    e.Data["item"] = book.FilePath;
                    errs.Add(e);
                    continue;
                }
                yield return book;
            }
            if (errs.Count > 0)
            {
                throw new AggregateException(errs.ToArray());
            }
        }


        /// <summary>
        /// Scans for all books in library and recreates database and directory structure
        /// </summary>
        public virtual IEnumerable<BookBase> Rescan()
        {
            List<Exception> errs = new List<Exception>();

            BookBase[] oldDB = Database.BOOKS.ToArray();
            Database.Drop("BOOKS");

            IEnumerable<string> bookPaths = Utils.Files.DirSearch(LibraryRoot).Where(x => CompatibleFiletypes.Contains(Path.GetExtension(x)));

            BookBase book;
            string dest;
            string destTemplate = FilePathTemplate();

            foreach (string filePath in bookPaths)
            {
                try
                {
                    book = BookBase.Auto(filePath);
                    dest = FormatFilePath(destTemplate, book);
                    Directory.CreateDirectory(Path.GetPathRoot(dest));
                    if (!File.Exists(dest))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));
                        File.Move(filePath, dest);
                    }
                    book.FilePath = RelativeFilepath(dest);

                    BookBase local = null;
                    if (book.ISBN != 0) local = oldDB.FirstOrDefault(x => x.ISBN == book.ISBN);
                    if (local != null)
                    {
                        book.Id = local.Id;
                        book.Series = local.Series;
                        book.SeriesNum = local.SeriesNum;
                    }
                    Database.AddBook(book);
                }
                catch (Exception e)
                {
                    e.Data["item"] = filePath;
                    errs.Add(e);
                    continue;
                }
                yield return book;
            }
            if (errs.Count > 0)
            {
                throw new AggregateException(errs.ToArray());
            }
        }

        /// <summary>
        /// Adds book to library directory and database, converting if neccesary. Preserves book.Id
        /// </summary>
        public abstract void ImportBook(BookBase book);

        /// <summary>
        /// Removes book from database and disk
        /// </summary>
        /// <param name="id"></param>
        public abstract void DeleteBook(int id);

        public void DeleteBook(BookBase book)
        {
            DeleteBook(book.Id);
        }

        /// <summary>
        /// Adds book to library directory and database, converting if neccesary
        /// </summary>
        public void ImportBook(string filePath)
        {
            ImportBook(BookBase.Auto(filePath));
        }
    }
}
