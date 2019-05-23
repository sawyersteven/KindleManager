using System.Collections.Generic;
using System.IO;
using System;

namespace Formats
{
    public abstract class BookBase : ReactiveUI.ReactiveObject
    {
        #region IBook impl
        public virtual string FilePath { get; set; }
        public virtual string Title { get; set; }
        public virtual string Language { get; set; }
        public virtual ulong ISBN { get; set; }
        public virtual string Author { get; set; }
        public virtual string Contributor { set; get; }
        public virtual string Publisher { get; set; }
        public virtual string[] Subject { get; set; }
        public virtual string Description { get; set; }
        private string _PubDate;
        public virtual string PubDate {
            get => _PubDate;
            set {
                value = Utils.Metadata.GetDate(value);
                _PubDate = value;
            }
        }
        public virtual string Rights { get; set; }

        public virtual int Id { get; set; }
        public virtual string Series { get; set; }
        public virtual float SeriesNum { get; set; }

        public virtual string DateAdded { get; set; }

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
                { "SeriesNum", SeriesNum.ToString() }
            };

            return p;
        }
    }
}
