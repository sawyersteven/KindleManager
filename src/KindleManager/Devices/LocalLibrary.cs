using ExtensionMethods;
using Formats;
using NLog;
using System;
using System.IO;
using System.Linq;

namespace KindleManager.Devices
{
    public class LocalLibrary : DeviceBase
    {
        public LocalLibrary(string dbFile)
        {
            Database = new Database(dbFile);
        }

        #region property overrides
        public override string Name { get => "Local Library"; }
        public override string Id { get => "LocalLibary"; }
        public override string Description { get; set; }
        public override Database Database { get; set; }
        public override string[] CompatibleFiletypes { get; } = new string[] { ".mobi", ".azw", ".azw3" };
        public override string LibraryRoot => App.Config.LibraryRoot;
        #endregion

        #region method overrides
        public override void Clean()
        {
            Utils.Files.CleanForward(LibraryRoot);
        }

        public override string AbsoluteFilePath(BookBase book)
        {
            return Path.GetFullPath(Path.Combine(LibraryRoot, book.FilePath));
        }

        public override bool Open() { return false; }

        public override string FilePathTemplate()
        {
            return Path.Combine(App.Config.LibraryRoot, App.Config.DirectoryFormat, "{Title}");
        }

        public override void DeleteBook(int id)
        {
            Database.BookEntry bookEntry = Database.BOOKS.FirstOrDefault(x => x.Id == id);
            if (bookEntry == null)
            {
                throw new Database.IDNotFoundException();
            }

            Database.RemoveBook(bookEntry);

            string filePath = AbsoluteFilePath(bookEntry);
            try
            {
                File.Delete(filePath);
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            catch (Exception e)
            {
                throw new Exception($"{bookEntry.Title} was removed from database but the file could not be deleted. {e.Message}");
            }

            Utils.Files.CleanBackward(Path.GetDirectoryName(filePath), LibraryRoot);
        }

        public override void ImportBook(BookBase book)
        {
            string template = Path.Combine(App.Config.DirectoryFormat, "{Title}");
            string destinationFile = Path.Combine(App.Config.LibraryRoot, template.DictFormat(book.Props())) + ".mobi";
            destinationFile = Utils.Files.MakeFilesystemSafe(destinationFile);

            Logger.Info("Importing {} from {} to {}", book.Title, book.FilePath, destinationFile);

            if (App.LocalLibrary.Database.BOOKS.Any(x => x.FilePath == destinationFile))
            {
                throw new IOException("File already exists in libary");
            }

            Directory.CreateDirectory(Directory.GetParent(destinationFile).FullName);
            if (!CompatibleFiletypes.Contains(Path.GetExtension(book.FilePath)))
            {
                int id = book.Id;
                book = Converters.ToMobi(book, destinationFile);
                book.Id = id;
            }
            else
            {
                File.Copy(book.FilePath, destinationFile);
            }

            book.FilePath = RelativeFilepath(destinationFile);
            App.LocalLibrary.Database.AddBook(book);
        }

        public override string RelativeFilepath(string absPath)
        {
            return absPath.Substring(LibraryRoot.Length + 1);
        }
        #endregion
    }
}
