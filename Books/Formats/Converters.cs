using System;
using System.IO;
using Formats;

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
        private void Merge(IBook donor, IBook recipient)
        {
            recipient.Title = donor.Title;
            recipient.Language = donor.Language;
            recipient.ISBN = donor.ISBN;

            recipient.Author = donor.Author;
            recipient.Contributor = donor.Contributor;
            recipient.Publisher = donor.Publisher;
            recipient.Subject = donor.Subject;
            recipient.Description = donor.Description;

            recipient.PubDate = donor.PubDate;
            recipient.Rights = donor.Rights;

            recipient.Series = donor.Series;
            recipient.SeriesNum = donor.SeriesNum;
            recipient.DateAdded = donor.DateAdded;
        }

        public static IBook NewIBook(string filepath)
        {
            IBook book;
            switch (Path.GetExtension(filepath))
            {
                case ".mobi":
                    book = new Formats.Mobi.Book(filepath);
                    break;
                case ".epub":
                    book = new Epub(filepath);
                    break;
                default:
                    throw new Exception("Unsupported file type");
            }
            return book;
        }

        public static IBook ToMobi(IBook input, string filePath = "")
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
