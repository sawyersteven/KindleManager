using Formats;
using System.IO;
using ExtensionMethods;
using System;
using System.Collections.Generic;

namespace Devices
{
    class Kindle : Device
    {

        public Kindle(string Letter, string Name, string Description)
        {
            DriveLetter = Letter;
            this.Name = Name;
            this.Description = Description;
            ConfigFile = Path.Combine(DriveLetter, "KindleManager.conf");
            DatabaseFile = Path.Combine(DriveLetter, "KindleManager.db");
            CompatibleFiletypes = new string[] { ".mobi" };
        }

        public override void SendBook(BookBase localBook)
        {
            if (Database.GetById(localBook.Id) != null)
            {
                throw new Exception($"Book already exists on kindle. [{localBook.Id}]");
            }

            Dictionary<string, string> bookMetadata = localBook.Props();

            string remoteFile = Path.Combine(Config.LibraryRoot, Config.DirectoryFormat, Path.GetFileName(localBook.FilePath));
            remoteFile = remoteFile.DictFormat(bookMetadata);
            remoteFile = remoteFile.NormPath();
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
