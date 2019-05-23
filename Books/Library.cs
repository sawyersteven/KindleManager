using System.Linq;
using Formats;
using System.IO;
using ExtensionMethods;
using System.Collections.ObjectModel;
using ReactiveUI.Fody.Helpers;

namespace Books
{
    class Library
    {
        /// <summary>
        /// Adds book to library directory and database
        /// </summary>
        public static void ImportBook(string filePath)
        {
            BookBase book = BookBase.Auto(filePath);

            string destinationFile = App.ConfigManager.config.LibraryFormat.DictFormat(book.Props());
            destinationFile = Path.Combine(App.ConfigManager.config.LibraryDir, destinationFile, book.Title) + ".mobi";
            destinationFile = Path.GetFullPath(destinationFile);

            if (App.Database.Library.Any(x => x.FilePath == destinationFile))
            {
                throw new IOException("File already exists in libary");
            }

            Directory.CreateDirectory(Directory.GetParent(destinationFile).FullName);
            if (Path.GetExtension(book.FilePath) != ".mobi")
            {
                book = Converters.ToMobi(book, destinationFile);
            }
            else
            {
                File.Copy(book.FilePath, destinationFile);
                book.FilePath = destinationFile;
            }
             
            App.Database.AddBook(book);
        }
    }
}
