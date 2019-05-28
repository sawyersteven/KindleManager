using System;
using System.IO;

/// <summary>
/// Collection of classes and methods to convert X to Y
/// </summary>
namespace Formats
{
    /// <summary>
    /// All methods write a new file to disk and return an instance of that
    ///     filetype's class.
    /// If filePath is passed the new file is created there, otherwise the new
    ///     file will be the same as IBook's filePath with the new extension.
    /// </summary>
    class Converters
    {        
        public static BookBase ToMobi(BookBase input, string filePath = "")
        {
            if (filePath == "")
            {
                filePath = input.FilePath;
            }
            if (filePath == "")
            {
                throw new ArgumentException($"Output filepath not provided.");
            }

            filePath = Path.ChangeExtension(filePath, ".mobi");


            Mobi.Builder mobibuilder = new Mobi.Builder(input, filePath);

            return mobibuilder.Convert();
        }
    }
}
