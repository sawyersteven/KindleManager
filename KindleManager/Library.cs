using ExtensionMethods;
using Formats;
using System.IO;
using System.Linq;

namespace KindleManager
{
    class Library
    {
        /// <summary>
        /// Adds book to library directory and database, converting if neccesary. Preserves book.Id
        /// </summary>
        /// <param name="book"></param>
        public static void ImportBook(BookBase book)
        {
            string template = Path.Combine(App.ConfigManager.config.LibraryFormat, "{Title}");
            string destinationFile = Path.Combine(App.ConfigManager.config.LibraryDir, template.DictFormat(book.Props())) + ".mobi";
            destinationFile = Utils.Files.MakeFilesystemSafe(destinationFile);

            if (App.Database.Library.Any(x => x.FilePath == destinationFile))
            {
                throw new IOException("File already exists in libary");
            }

            Directory.CreateDirectory(Directory.GetParent(destinationFile).FullName);
            if (!Resources.CompatibleFiletypes.Contains(Path.GetExtension(book.FilePath)))
            {
                int id = book.Id;
                book = Converters.ToMobi(book, destinationFile);
                book.Id = id;
            }
            else
            {
                File.Copy(book.FilePath, destinationFile);
                book.FilePath = destinationFile;
            }

            try
            {
                Formats.Mobi.Book m = (Formats.Mobi.Book)book;
            }
            catch (System.Exception e)
            {
                throw new System.Exception($"Conversion to Mobi failed: {e.Message}");
            }
            App.Database.AddBook(book);
        }

        /// <summary>
        /// Adds book to library directory and database, converting if neccesary
        /// </summary>
        public static void ImportBook(string filePath)
        {
            ImportBook(BookBase.Auto(filePath));
        }
    }
}
