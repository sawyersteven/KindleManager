using ExtensionMethods;
using Formats;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Devices
{
    public abstract class Device
    {
        public virtual string DriveLetter { get; set; }
        public virtual string Name { get; set; }
        public virtual string Description { get; set; }
        public virtual string ConfigFile { get; set; }
        public virtual string DatabaseFile { get; set; }
        public ConfigManager<Config.DeviceConfig> ConfigManager { get; set; }
        public virtual string[] CompatibleFiletypes { get; set; }
        public virtual bool FirstUse
        {
            get
            {
                return !File.Exists(Path.Combine(DriveLetter, "KindleManager.conf"));
            }
        }
        public virtual KindleManager.Database Database { get; set; }

        /// <summary>
        /// Gets absolute path to book on device.
        /// Since drive letters/mount points will change only a relative path
        ///     is stored in the db.
        /// </summary>
        public virtual string AbsoluteFilePath(BookBase book)
        {
            return Path.Combine(DriveLetter, book.FilePath);
        }

        //public void WriteConfig(DeviceConfig c)
        //{
        //    File.WriteAllText(ConfigFile, JsonConvert.SerializeObject(c));
        //    Config = c;
        //}

        /// <summary>
        /// Opens device config, database, etc for read/write
        /// </summary>
        public virtual void Open()
        {

            ConfigManager = new ConfigManager<Config.DeviceConfig>(ConfigFile);
            DatabaseFile = Path.Combine(DriveLetter, "KindleManager.db");
            Database = new Database(DatabaseFile);
        }

        /// <summary>
        /// Creates new config and database on device
        /// </summary>
        /// <param name="newDevice">If device</param>
        public virtual void Init(bool newDevice)
        {
            //try
            //{
            //    Directory.CreateDirectory(Path.Combine(DriveLetter, ConfigManager.config.LibraryRoot));
            //}
            //catch (Exception e)
            //{
            //    throw new Exception($"Unable to create root library directory. [{e.Message}]");
            //}

            //try
            //{
            //    Database = new KindleManager.Database(Path.Combine(DriveLetter, "KindleManager.db"));
            //}
            //catch (Exception e)
            //{
            //    throw new Exception($"Unable to create new database file. [{e.Message}]");
            //}
        }

        public string FormatPath(BookBase book)
        {
            Dictionary<string, string> props = book.Props();
            string p = Path.Combine(DriveLetter, ConfigManager.config.LibraryRoot.DictFormat(props), ConfigManager.config.DirectoryFormat.DictFormat(props), (ConfigManager.config.ChangeTitleOnSync ? ConfigManager.config.TitleFormat : "{Title}").DictFormat(props));
            return Path.GetFullPath(p + Path.GetExtension(book.FilePath));
        }

        /// <summary>
        /// Moves and names books based on config settings
        /// </summary>
        public virtual IEnumerable<string> ReorganizeLibrary()
        {
            List<Exception> errs = new List<Exception>();
            foreach (BookBase book in Database.BOOKS)
            {
                yield return book.Title;
                string origPath = AbsoluteFilePath(book);
                string template = Path.Combine(DriveLetter, Config.LibraryRoot, Config.DirectoryFormat, (Config.ChangeTitleOnSync ? Config.TitleFormat : "{Title}"));
                string dest = FormatFilePath(template, book);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    if (dest != book.FilePath)
                    {
                        File.Move(origPath, dest);
                    }
                    book.FilePath = dest.Substring(DriveLetter.Length);
                    Database.UpdateBook(book);
                }
                catch (KindleManager.Database.IDNotFoundException) { } // Don't care;
                catch (Exception e)
                {
                    e.Data["item"] = book.Title;
                    errs.Add(e);
                }
            }
            if (errs.Count > 0)
            {
                throw new AggregateException(errs.ToArray());
            }
        }

        /// <summary>
        /// Finds all compatible files on device, moves/renames them, and adds to database
        /// Will create a *new* database, all existing information will be lost
        /// </summary>
        public virtual IEnumerable<string> RecreateLibraryAndDatabse()
        {
            List<Exception> errors = new List<Exception>();
            string destTemplate = Path.Combine(DriveLetter, Config.LibraryRoot, Config.DirectoryFormat, "{Title}");

            Database.ScorchedEarth();
            IEnumerable<string> books = Utils.Files.DirSearch(DriveLetter).Where(x => CompatibleFiletypes.Contains(Path.GetExtension(x)));
            BookBase book;
            string dest;
            foreach (string filepath in books)
            {
                yield return filepath;
                try
                {
                    book = BookBase.Auto(filepath);
                    if (ConfigManager.config.ChangeTitleOnSync)
                    {
                        book.Title = ConfigManager.config.TitleFormat.DictFormat(book.Props());
                        book.WriteMetadata();
                    }
                    dest = FormatFilePath(destTemplate, book);
                    book.FilePath = dest.Substring(Path.GetPathRoot(dest).Length);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    if (!File.Exists(dest))
                    {
                        {
                            File.Move(filepath, dest);
                        }
                    }
                    BookBase local = KindleManager.App.Database.FindMatch(book);

                    if (local != null && !Database.BOOKS.Any(x => x.Id == local.Id))
                    {
                        book.Id = local.Id;
                        book.Series = local.Series;
                        book.SeriesNum = local.SeriesNum;
                    }
                    Database.AddBook(book);
                }
                catch (Exception e)
                {
                    e.Data.Add("item", Path.GetFileName(filepath));
                    errors.Add(e);
                }
            }

            Utils.Files.CleanForward(Path.Combine(DriveLetter, ConfigManager.config.LibraryRoot));

            if (errors.Count > 0)
            {
                throw new AggregateException(errors.ToArray());
            }

        }

        /// <summary>
        /// Removes empty directories from library dir
        /// </summary>
        public virtual void CleanLibrary()
        {
            Utils.Files.CleanForward(Path.Combine(DriveLetter, ConfigManager.config.LibraryRoot));
        }

        public virtual void DeleteBook(int id)
        {
            KindleManager.Database.BookEntry b = Database.BOOKS.FirstOrDefault(x => x.Id == id);
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

            Utils.Files.CleanBackward(Path.GetDirectoryName(file), Path.Combine(DriveLetter, ConfigManager.config.LibraryRoot));
        }

        public abstract void SendBook(BookBase localbook);
    }
}
