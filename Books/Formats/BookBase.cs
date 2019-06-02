using System.Collections.Generic;
using System.IO;
using System;

namespace Formats
{
    public abstract class BookBase : ReactiveUI.ReactiveObject
    {
        #region IBook impl
        public virtual string FilePath { get; set; } = "";
        public virtual string Title { get; set; } = "";
        public virtual string Language { get; set; } = "";
        public virtual ulong ISBN { get; set; }
        public virtual string Author { get; set; } = "";
        public virtual string Contributor { set; get; } = "";
        public virtual string Publisher { get; set; } = "";
        public virtual string[] Subject { get; set; }
        public virtual string Description { get; set; } = "";
        private string _PubDate = "";
        public virtual string PubDate {
            get => _PubDate;
            set {
                value = Utils.Metadata.GetDate(value);
                _PubDate = value;
            }
        }
        public virtual string Rights { get; set; } = "";

        public virtual int Id { get; set; }
        public virtual string Series { get; set; } = "";
        private string _SeriesNum = "";
        public virtual float SeriesNum {
            get => float.TryParse(_SeriesNum, out float f) ? f : 0;
            set
            {
                _SeriesNum = value == 0 ? "" : value.ToString();
            }
        }

        public virtual string DateAdded { get; set; } = "";

        public abstract string TextContent();
        public abstract byte[][] Images();
        public abstract void WriteMetadata();

        #endregion

        public BookBase() { }

        public static BookBase Auto(string filepath)
        {
            BookBase b = null;
            switch (Path.GetExtension(filepath))
            {
                case ".mobi":
                    b = new Formats.Mobi.Book(filepath);
                    break;
                case ".epub":
                    b = new Formats.Epub.Book(filepath);
                    break;
            }
            if (b == null)
            {
                throw new NotImplementedException($"File type {Path.GetExtension(filepath)} is not supported");
            }
            return b;
        }

        public static void Merge(BookBase donor, BookBase recipient)
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

        public Dictionary<string, string> Props()
        {
            Dictionary<string, string> p = new Dictionary<string, string>
            {
                { "Title", Title },
                { "ISBN", ISBN.ToString() },
                { "Author", Author },
                { "Publisher", Publisher },
                { "PubDate", PubDate },
                { "Series", Series },
                { "SeriesNum", SeriesNum == 0 ? "" : SeriesNum.ToString() }
            };

            return p;
        }
    }
}
