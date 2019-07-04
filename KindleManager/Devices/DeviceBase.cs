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
        public DeviceConfig Config { get; set; }
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

        public void WriteConfig(DeviceConfig c)
        {
            File.WriteAllText(ConfigFile, JsonConvert.SerializeObject(c));
            Config = c;
        }

        public void LoadDatabase()
        {
            Database = new KindleManager.Database(DatabaseFile);
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
        /// Formats a new filepath string based on template. Removes illegal filesystem chars.
        /// </summary>
        public string FormatFilePath(string template, BookBase book)
        {
            return Utils.Files.MakeFilesystemSafe(template.DictFormat(book.Props()) + Path.GetExtension(book.FilePath));
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
                catch (KindleManager.IDNotFoundException) { } // Don't care;
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

            int nextId = KindleManager.App.Database.NextID();

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
                    if (Config.ChangeTitleOnSync)
                    {
                        book.Title = Config.TitleFormat.DictFormat(book.Props());
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

                    if (local != null && !Database.Library.Any(x => x.Id == local.Id))
                    {
                        book.Id = local.Id;
                        book.Series = local.Series;
                        book.SeriesNum = local.SeriesNum;
                    }
                    else
                    {
                        book.Id = nextId;
                        nextId++;
                    }

                    Database.AddBook(book);
                }
                catch (Exception e)
                {
                    e.Data.Add("item", Path.GetFileName(filepath));
                    errors.Add(e);
                }
            }

            Utils.Files.CleanForward(Path.Combine(DriveLetter, Config.LibraryRoot));

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

        public virtual void DeleteBook(int id)
        {
            KindleManager.Database.BookEntry b = Database.Library.FirstOrDefault(x => x.Id == id);
            if (b == null)
            {
                throw new ArgumentException($"Book with Id [{id}] not found in library");
            }
            string file = AbsoluteFilePath(b);
            try
            {
                File.Delete(file);
            }
            catch (FileNotFoundException _) { }
            catch (DirectoryNotFoundException _) { }

            Database.RemoveBook(b);

            Utils.Files.CleanBackward(Path.GetDirectoryName(file), Path.Combine(DriveLetter, Config.LibraryRoot));
        }

        public abstract void SendBook(BookBase localbook);
    }
}
