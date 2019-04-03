using System;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Xml;

namespace Formats
{
    public class Epub : IBook

    {
        public Epub(string filepath)
        {
            FilePath = filepath;

            using (var file = File.OpenRead(FilePath))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
            {
                ZipArchiveEntry origContainerOpf = zip.Entries.FirstOrDefault(x => x.Name == "content.opf");
                if (origContainerOpf == null) throw new Exception("Metadata file content.opf not found, epub may be corrupt");

                XmlDocument containerXML = new XmlDocument();
                using (Stream s = origContainerOpf.Open()) { containerXML.Load(s); }

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(containerXML.NameTable);
                nsmgr.AddNamespace("p", containerXML.DocumentElement.NamespaceURI);

                XmlNode metadataNode = containerXML.SelectSingleNode("//p:package/p:metadata", nsmgr);
                nsmgr.AddNamespace("dc", metadataNode.GetNamespaceOfPrefix("dc"));
                nsmgr.AddNamespace("opf", metadataNode.GetNamespaceOfPrefix("opf"));

                Title = metadataNode.SelectSingleNode("dc:title", nsmgr).InnerText;
                Author = metadataNode.SelectSingleNode("dc:creator", nsmgr).InnerText;
                Publisher = metadataNode.SelectSingleNode("dc:publisher", nsmgr).InnerText;
                PubDate = metadataNode.SelectSingleNode("dc:date", nsmgr).InnerText.Substring(0, 10);
                ISBN = ulong.Parse(metadataNode.SelectSingleNode("dc:identifier[@opf:scheme='ISBN']", nsmgr).InnerText);
            }
        }

        public void Print()
        {
            Console.WriteLine($@"
                Id: {Id}
                FilePath: {FilePath}
                Type: {Type}
                Title: {Title}
                Author: {Author}
                Publisher: {Publisher}
                PubDate: {PubDate}
                ISBN: {ISBN}
                Series: {Series}
                SeriesNum: {SeriesNum}
                DateAdded: {DateAdded}
            ");
        }

        #region IBook impl
        public int Id { get; set; }
        public string FilePath { get; set; }
        public string Type { get => "EPUB"; }

        private string _Title;
        public string Title {
            get => _Title;
            set
            {
                _Title = value;
            }
        }

        private string _Author;
        public string Author {
            get => _Author;
            set
            {
                _Author = value;
            }
        }

        private string _Publisher;
        public string Publisher {
            get => _Publisher;
            set
            {
                _Publisher = value;
            }
        }

        private string _PubDate;
        public string PubDate {
            get => _PubDate;
            set
            {
                value = value.Substring(0, 10);
                _PubDate = value;
            }
        }

        private ulong _ISBN;
        public ulong ISBN {
            get => _ISBN;
            set
            {
                _ISBN = value;
            }
        }

        // local db only, not parsed when instantiated
        private string _Series;
        public string Series {
            get => _Series;
            set
            {
                _Series = value;
            }
        }

        private float _SeriesNum;
        public float SeriesNum {
            get => _SeriesNum;
            set
            {
                _SeriesNum = value;
            }
        }

        public string DateAdded { get; set; }

        public void Write()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}