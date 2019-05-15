using Formats;
using System.IO;
using ExtensionMethods;
using System;

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
        }

        public override void SendBook(BookBase localBook)
        {
            string remoteFile = Path.Combine(DriveLetter, Config.LibraryRoot, Config.DirectoryFormat);

            Books.Database.BookEntry dbrow = Books.App.Database.GetByFileName(remoteFile);
            if (dbrow != null)
            {
                throw new Exception("Book already exists on kindle");
            }

            if (Config.ChangeTitleOnSync)
            {
                remoteFile = Path.Combine(remoteFile, Config.TitleFormat) + ".mobi";
            }
            else
            {
                remoteFile = Path.Combine(remoteFile, Path.GetFileName(localBook.FilePath));
            }

            remoteFile = remoteFile.DictFormat(localBook.Props());

            remoteFile = Path.Combine(remoteFile); // normalizes path

            Directory.CreateDirectory(Path.GetDirectoryName(remoteFile));

            File.Copy(localBook.FilePath, remoteFile);

            localBook.FilePath = remoteFile;
            this.Database.AddBook(localBook);
        }
    }
}
