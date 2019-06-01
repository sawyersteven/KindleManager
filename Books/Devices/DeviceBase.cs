using System;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;
using ExtensionMethods;
using Formats;

namespace Devices
{
    public abstract class Device
    {

        public virtual string DriveLetter { get; set; }
        public virtual string Name { get; set; }
        public virtual string Description { get; set; }
        public virtual string ConfigFile { get; set; }
        public virtual string DatabaseFile { get; set; }
        public DeviceConfig Config { get; set; }
        public virtual string[] CompatibleFiletypes { get; set; }
        public virtual bool FirstUse
        {
            get
            {
                return !File.Exists(Path.Combine(DriveLetter, "KindleManager.conf"));
            }
        }
        public virtual Books.Database Database { get; set; }

        public void WriteConfig(DeviceConfig c)
        {
            File.WriteAllText(ConfigFile, JsonConvert.SerializeObject(c));
            Config = c;
        }

        public void LoadDatabase()
        {
            Database = new Books.Database(DatabaseFile);
        }

        public void Init()
        {
            if (File.Exists(ConfigFile))
            {
                Config = JsonConvert.DeserializeObject<DeviceConfig>(File.ReadAllText(ConfigFile));
            }
            else
            {
                Config = new DeviceConfig();
                WriteConfig(Config);
            }

            LoadDatabase();
        }

        /// <summary>
        /// Moves and names books based on config settings
        /// </summary>
        public virtual IEnumerable<string> ReorganizeLibrary()
        {
            List<Exception> errs = new List<Exception>();
            foreach (Formats.BookBase book in this.Database.Library)
            {
                yield return book.Title;
                try
                {
                    string origPath = Path.Combine(DriveLetter, book.FilePath);
                    string newPath = "";
                    if (Config.ChangeTitleOnSync)
                    {
                        newPath = (Path.Combine(DriveLetter, Config.LibraryRoot, Config.DirectoryFormat, Config.TitleFormat) + Path.GetExtension(book.FilePath)).DictFormat(book.Props());
                    }
                    else
                    {
                        newPath = (Path.Combine(DriveLetter, Config.LibraryRoot, Config.DirectoryFormat, "{Title}") + Path.GetExtension(book.FilePath)).DictFormat(book.Props());
                    }
                    newPath = newPath.NormPath();

                    Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                    File.Move(origPath, newPath);
                    book.FilePath = newPath.Substring(DriveLetter.Length);
                    Books.App.Database.UpdateBook(book);
                }
                catch (Exception e)
                {
                    e.Data["File"] = book.Title;
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
        public virtual IEnumerable<string> RecreateLibraryAndDatabse(){
            List<Exception> errors = new List<Exception>();
            string destTemplate = Path.Combine(DriveLetter, Config.LibraryRoot, Config.DirectoryFormat, "{Title}");

            string dbfp = Books.App.Database.DBFile;
            Books.App.Database.Dispose();
            File.Delete(dbfp);

            IEnumerable<string> books = Utils.Files.DirSearch(Path.Combine(DriveLetter, Config.LibraryRoot)).Where(x => CompatibleFiletypes.Contains(Path.GetExtension(x)));

            BookBase book;
            foreach (string filepath in books)
            {
                yield return filepath;
                try
                {
                    book = Formats.BookBase.Auto(filepath);
                    if (Config.ChangeTitleOnSync)
                    {
                        book.Title = Config.TitleFormat.DictFormat(book.Props());
                        book.WriteMetadata();
                    }
                    string dest = destTemplate.DictFormat(book.Props()) + Path.GetExtension(filepath);
                    book.FilePath = dest.Substring(Path.GetPathRoot(dest).Length);
                    Directory.CreateDirectory(Path.GetDirectoryName(filepath));
                    File.Move(filepath, dest);

                    Books.Database.BookEntry local = Books.App.Database.Library.FirstOrDefault(x => x.ISBN == book.ISBN);
                    if (local != null)
                    {
                        book.Id = local.Id;
                    }

                    Database.AddBook(book);
                }
                catch (Exception e)
                {
                    errors.Add(e);
                }
            }

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
            List<Exception> errs = new List<Exception>();

            string[] dirs = Utils.Files.DirSearch(Path.Combine(DriveLetter, Config.LibraryRoot), true);

            foreach (string dir in dirs)
            {
                if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    try
                    {
                        Directory.Delete(dir);
                    }
                    catch (Exception e)
                    {
                        errs.Add(e);
                    }
                }
            }
            if (errs.Count > 0)
            {
                throw new AggregateException(errs.ToArray());
            }
        }

        public abstract void SendBook(Formats.BookBase localbook);
    }
}
