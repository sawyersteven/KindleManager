using System.Collections.Generic;

namespace Formats
{
    public abstract class BookBase
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

        public Dictionary<string, string> Props()
        {
            Dictionary<string, string> p = new Dictionary<string, string>();

            p.Add("Title", Title);
            p.Add("ISBN", ISBN.ToString());
            p.Add("Author", Author);
            p.Add("Publisher", Publisher);
            p.Add("PubDate", PubDate);
            p.Add("Series", Series);
            p.Add("SeriesNum", SeriesNum.ToString());

            return p;
        }
    }
}
