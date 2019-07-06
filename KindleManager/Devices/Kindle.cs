using ExtensionMethods;
using Formats;
using System;
using System.Collections.Generic;
using System.IO;

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
            remoteFile = Path.GetFullPath(remoteFile);
            string remoteFileAbs = Path.Combine(this.DriveLetter, remoteFile);

            Directory.CreateDirectory(Path.GetDirectoryName(remoteFileAbs));
            File.Copy(localBook.FilePath, remoteFileAbs);

            localBook.FilePath = remoteFile;
            this.Database.AddBook(localBook);

            if (Config.ChangeTitleOnSync)
            {
                BookBase remoteBook = new Formats.Mobi.Book(remoteFile);
                remoteBook.Title = Config.TitleFormat.DictFormat(bookMetadata);
                remoteBook.WriteMetadata();
            }
        }
    }
}
