using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Formats;

/// <summary>
/// Collection of classes and methods to convert X to Y
/// </summary>
namespace Converters
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


        public static void ToMobi(IBook input, string filePath = "")
        {
            if (filePath == "")
            {
                filePath = input.FilePath;
            }
            if (filePath == "")
            {
                throw new ArgumentException($"Output filepath not provided.");
            }

            filePath = System.IO.Path.ChangeExtension(filePath, ".mobi");


            Formats.MobiBuilder.Convert(input, filePath);
            return;
        }
    }
}
