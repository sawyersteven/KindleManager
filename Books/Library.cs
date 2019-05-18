using System.Linq;
using Formats;
using System.IO;
using ExtensionMethods;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading;

namespace Books
{
    class Library
    {

        public static ObservableCollection<Database.BookEntry> Books { get; } = App.Database.Library;

        /// <summary>
        /// Adds book to library directory and database
        /// </summary>
        public static void ImportBook(string filePath)
        {
            BookBase book = Converters.NewIBook(filePath);

            string destinationFile = App.ConfigManager.config.LibraryFormat.DictFormat(book.Props());
            destinationFile = Path.Combine(App.ConfigManager.config.LibraryDir, destinationFile, book.Title) + ".mobi";
            destinationFile = Path.GetFullPath(destinationFile);

            if (Books.Any(x => x.FilePath == destinationFile))
            {
                throw new IOException("File already exists in libary");
            }

            if (Path.GetExtension(book.FilePath) != ".mobi")
            {
                book = Converters.ToMobi(book, destinationFile);
            }

            App.Database.AddBook(book);

            Directory.CreateDirectory(Directory.GetParent(destinationFile).FullName);
            File.Copy(book.FilePath, destinationFile);
        }
    }
}
