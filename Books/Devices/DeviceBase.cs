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
            foreach (Formats.BookBase book in Books.App.Database.Library)
            {
                yield return book.Title;
                try
                {
                    string origPath = book.FilePath;
                    string newPath = (Path.Combine(DriveLetter, Config.LibraryRoot, Config.DirectoryFormat, Config.TitleFormat) + Path.GetExtension(book.FilePath)).DictFormat(book.Props());
                    Directory.CreateDirectory(Path.GetDirectoryName(origPath));
                    File.Move(origPath, newPath);
                    book.FilePath = newPath;
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
                    if (Config.ChangeTitleOnSync){
                        book.Title = Config.TitleFormat.DictFormat(book.Props());
                        book.WriteMetadata();
                    }
                    string dest = destTemplate.DictFormat(book.Props()) + Path.GetExtension(filepath);
                    book.FilePath = dest.Substring(Path.GetPathRoot(dest).Length);
                    Directory.CreateDirectory(Path.GetDirectoryName(filepath));
                    File.Move(filepath, dest);
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

        public abstract void SendBook(Formats.BookBase localbook);
    }
}
