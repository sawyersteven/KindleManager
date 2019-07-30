using ExtensionMethods;
using Formats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KindleManager.Devices
{
    /// <summary>
    /// Device for basic non-proprietary filesystem readers
    /// </summary>
    public class FSDevice : DeviceBase
    {
        public string DriveLetter { get; set; }

        public string ConfigFile
        {
            get => Path.Combine(DriveLetter, "KindleManager.conf");
        }

        public string DatabaseFile { get; set; }

        public Config.FSDeviceConfig Config { get; set; }

        public bool FirstUse
        {
            get
            {
                return !File.Exists(Path.Combine(DriveLetter, "KindleManager.conf"));
            }
        }

        #region prop overrides
        public override string Description { get; set; }
        public override string Name { get; }
        public override string Id { get; }
        public override string[] CompatibleFiletypes { get; } = new string[] { ".mobi", ".azw", ".azw3" };
        public override Database Database { get; set; }
        public override string LibraryRoot => Path.Combine(DriveLetter, Config.LibraryRoot);
        #endregion

        #region method overrides
        public override void Clean()
        {
            Utils.Files.CleanForward(LibraryRoot);
        }

        /// <summary>
        /// Creates string-formattable absolute filepath template according to config.
        /// Does not apply file extension.
        /// </summary>
        public override string FilePathTemplate()
        {
            return Path.GetFullPath(Path.Combine(DriveLetter, Config.LibraryRoot, Config.DirectoryFormat, Config.ChangeTitleOnSync ? Config.TitleTemplate : "{Title}"));
        }

        /// <summary>
        /// Turns absolute file path into path relative to library root.
        /// </summary>
        /// <param name="absPath"></param>
        /// <returns></returns>
        public override string RelativeFilepath(string absPath)
        {
            return absPath.Substring(Path.Combine(DriveLetter, Config.LibraryRoot).Length + 1);
        }

        /// <summary>
        /// Gets absolute path to book on device.
        /// Since drive letters/mount points will change only a relative path is stored in the db.
        /// </summary>
        public override string AbsoluteFilePath(BookBase book)
        {
            return Path.GetFullPath(Path.Combine(DriveLetter, Config.LibraryRoot, book.FilePath));
        }

        /// <summary>
        /// Moves and names books based on config settings
        /// </summary>
        public override IEnumerable<BookBase> Reorganize()
        {
            List<Exception> errs = new List<Exception>();

            IEnumerator<BookBase> iterator = base.Reorganize().GetEnumerator();

            while (true)
            {
                bool next = false;
                try
                {
                    next = iterator.MoveNext();
                }
                catch (AggregateException e)
                {
                    errs.AddRange(e.InnerExceptions);
                }
                if (!next) break;

                BookBase book = iterator.Current;
                yield return book;

                if (Config.ChangeTitleOnSync)
                {
                    try
                    {
                        book.Title = Config.TitleTemplate.DictFormat(book.Props());

                        book.WriteMetadata();
                        //Database.UpdateBook(book);
                    }
                    catch (Exception e)
                    {
                        e.Data["item"] = book.FilePath;
                        errs.Add(e);
                    }
                }
            }

            if (errs.Count > 0) throw new AggregateException(errs.ToArray());
        }

        /// <summary>
        /// Finds all compatible files on device, moves/renames them, and adds to database
        /// Will create a *new* database, all existing information will be lost
        /// </summary>
        public override IEnumerable<BookBase> Rescan()
        {
            List<Exception> errs = new List<Exception>();

            IEnumerator<BookBase> iterator = base.Rescan().GetEnumerator();

            while (true)
            {
                bool next = false;
                try
                {
                    next = iterator.MoveNext();
                }
                catch (AggregateException e)
                {
                    errs.AddRange(e.InnerExceptions);
                }
                if (!next) break;

                BookBase book = iterator.Current;
                yield return book;

                if (Config.ChangeTitleOnSync)
                {
                    try
                    {
                        book.Title = Config.TitleTemplate.DictFormat(book.Props());
                        book.FilePath = AbsoluteFilePath(book);
                        book.WriteMetadata();

                    }
                    catch (Exception e)
                    {
                        e.Data["item"] = book.Title;
                        errs.Add(e);
                    }
                }
            }
            if (errs.Count > 0) throw new AggregateException(errs.ToArray());
        }

        #endregion

        public FSDevice(string driveLetter, string name, string description, string id)
        {
            DatabaseFile = Path.Combine(driveLetter, "KindleManager.db");
            DriveLetter = driveLetter;
            Name = name;
            Description = $"{DriveLetter} {description}";
            Id = id;
        }

        /// <summary>
        /// Opens device config, database, etc for read/write
        /// </summary>
        public override bool Open()
        {
            bool firstUse = !File.Exists(ConfigFile);
            Config = new Config.FSDeviceConfig(ConfigFile);
            Config = new Config.FSDeviceConfig(ConfigFile);
            Config.Write();
            Database = new Database(DatabaseFile);
            return firstUse;
        }

        public override void DeleteBook(int id)
        {
            Database.BookEntry b = Database.BOOKS.FirstOrDefault(x => x.Id == id);
            if (b == null)
            {
                throw new ArgumentException($"Book with Id [{id}] not found in library");
            }
            string file = AbsoluteFilePath(b);
            try
            {
                File.Delete(file);
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }

            Database.RemoveBook(b);

            Utils.Files.CleanBackward(Path.GetDirectoryName(file), Path.Combine(DriveLetter, Config.LibraryRoot));
        }

        public override void ImportBook(BookBase localBook)
        {
            Logger.Info("Copying {} to Kindle.", localBook.Title);
            BookBase remoteBook = Database.BOOKS.FirstOrDefault(x => x.Id == localBook.Id);
            if (remoteBook != null)
            {
                Logger.Info("{}[{}] already exists on Kindle, copying metadata from pc library.", localBook.Title, localBook.Id);
                UpdateBookMetadata(localBook);
                return;
            }

            Dictionary<string, string> bookMetadata = localBook.Props();

            string remoteFileAbs = Path.Combine(Config.LibraryRoot, Config.DirectoryFormat, Path.GetFileName(localBook.FilePath)).DictFormat(bookMetadata);
            remoteFileAbs = Utils.Files.MakeFilesystemSafe(Path.Combine(this.DriveLetter, remoteFileAbs));
            string remoteFileRelative = RelativeFilepath(remoteFileAbs);

            Directory.CreateDirectory(Path.GetDirectoryName(remoteFileAbs));

            if (File.Exists(remoteFileAbs))
            {
                Database.BookEntry remoteEntry = Database.BOOKS.FirstOrDefault(x => x.FilePath == remoteFileRelative);
                if (remoteEntry == null) // file exists but not in database
                {
                    Logger.Info("File {} exists but is not in Kindle's database. Overwriting with local copy.", remoteFileAbs);
                    File.Delete(remoteFileAbs);
                    File.Copy(localBook.FilePath, remoteFileAbs);
                }
                else
                {
                    Logger.Info("{} exists on Kindle with ID {}. ID will be changed to {} to match local database and metadata will be copied from pc library.", localBook.Title, remoteEntry.Id, localBook.Id);
                    Database.ChangeBookId(remoteEntry, localBook.Id);
                    remoteEntry.UpdateMetadata(localBook);
                }
            }
            else
            {
                Logger.Info("Copying {} to {}", localBook.FilePath, remoteFileAbs);
                Directory.CreateDirectory(Path.GetDirectoryName(remoteFileAbs));
                File.Copy(localBook.FilePath, remoteFileAbs);
            }

            remoteBook = new Formats.Mobi.Book(remoteFileAbs);
            BookBase.Merge(localBook, remoteBook);
            remoteBook.Id = localBook.Id;
            remoteBook.FilePath = remoteFileRelative;
            this.Database.AddBook(remoteBook);
            remoteBook.FilePath = remoteFileAbs;

            if (Config.ChangeTitleOnSync)
            {
                remoteBook.Title = Config.TitleTemplate.DictFormat(bookMetadata);
                Logger.Info("Changing title of {} to {}", localBook.Title, remoteBook.Title);
                remoteBook.WriteMetadata();
            }

        }
    }
}
