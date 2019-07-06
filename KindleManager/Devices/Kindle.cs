using ExtensionMethods;
using Formats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Devices
{
    class Kindle : Device
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public Kindle(string Letter, string Name, string Description)
        {
            DriveLetter = Letter;
            this.Name = Name;
            this.Description = Description;
            ConfigFile = Path.Combine(DriveLetter, "KindleManager.conf");
            DatabaseFile = Path.Combine(DriveLetter, "KindleManager.db");
            CompatibleFiletypes = new string[] { ".mobi", ".azw", ".azw3" };
        }

        public override void SendBook(BookBase localBook)
        {
            Logger.Info("Copying {} to Kindle", localBook.Title);
            if (Database.GetById(localBook.Id) != null)
            {
                Logger.Info("{}[{}] already exists on Kindle, copying metadata from pc library.", localBook.Title, localBook.Id);
                throw new Exception($"Book already exists on kindle. [{localBook.Id}]");
            }

            Dictionary<string, string> bookMetadata = localBook.Props();

            string remoteFile = Path.Combine(Config.LibraryRoot, Config.DirectoryFormat, Path.GetFileName(localBook.FilePath));
            remoteFile = remoteFile.DictFormat(bookMetadata);
            remoteFile = Path.GetFullPath(Path.Combine(this.DriveLetter, remoteFile));

            Directory.CreateDirectory(Path.GetDirectoryName(remoteFile));


            if (File.Exists(remoteFile))
            {
                KindleManager.Database.BookEntry remoteEntry = this.Database.Library.FirstOrDefault(x => x.FilePath == remoteFile);
                if (remoteEntry == null) // file exists but not in database
                {
                    Logger.Info("File {} exists but is not in Kindle's library. Overwriting...", remoteFile);
                    File.Delete(remoteFile);
                    File.Copy(localBook.FilePath, remoteFile);
                }
                else
                {
                    Logger.Info("{} exists on Kindle with ID {}. ID will be changed to {} to match local database and metadata will be copied from pc library.");
                    Database.ChangeBookId(remoteEntry, localBook.Id);
                    remoteEntry.Id = localBook.Id;
                    Database.UpdateBook(remoteEntry);
                    remoteEntry.UpdateMetadata(localBook);
                }
            }
            else
            {
                Logger.Info("Copying {} to {}", localBook.FilePath, remoteFile);
                File.Copy(localBook.FilePath, remoteFile);
            }

            BookBase remoteBook = new Formats.Mobi.Book(remoteFile);
            remoteBook.Id = localBook.Id;

            if (Config.ChangeTitleOnSync)
            {
                remoteBook.Title = Config.TitleFormat.DictFormat(bookMetadata);
                Logger.Info("Changing title of {} to {}", localBook.Title, remoteBook.Title);
                remoteBook.WriteMetadata();
            }
            remoteBook.FilePath = remoteBook.FilePath.Substring(Path.GetPathRoot(remoteFile).Length);
            this.Database.AddBook(remoteBook);
        }
    }
}
