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

            using (var zip = ZipFile.Open(FilePath, ZipArchiveMode.Read))
            {
                ZipArchiveEntry origMetadataOpf = zip.Entries.FirstOrDefault(x => x.Name.EndsWith(".opf"));
                if (origMetadataOpf == null) throw new Exception("OPF metadata file not found, epub may be corrupt");

                XmlDocument metadataXml = new XmlDocument();
                using (Stream s = origMetadataOpf.Open()) { metadataXml.Load(s); }

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(metadataXml.NameTable);
                nsmgr.AddNamespace("p", metadataXml.DocumentElement.NamespaceURI);

                XmlNode metadataNode = metadataXml.SelectSingleNode("//p:package/p:metadata", nsmgr);
                nsmgr.AddNamespace("dc", metadataNode.GetNamespaceOfPrefix("dc"));
                nsmgr.AddNamespace("opf", metadataNode.GetNamespaceOfPrefix("opf"));

                Title = ReadNode(metadataNode, "dc:title", nsmgr);
                Author = ReadNode(metadataNode, "dc:creator", nsmgr);
                Publisher = ReadNode(metadataNode, "dc:publisher", nsmgr);
                PubDate = ReadNode(metadataNode, "dc:date", nsmgr);
                ulong.TryParse(ReadNode(metadataNode, "dc:identifier[@opf:scheme='ISBN']", nsmgr), out ulong id);
                ISBN = id;
            }
        }

        /// <summary>
        /// Reads text from node. Returns empty string if node doesn't exist.
        /// </summary>
        private string ReadNode(XmlNode node, string xpath, XmlNamespaceManager nsmgr)
        {
            XmlNode target = node.SelectSingleNode(xpath, nsmgr);
            return (target == null) ? "" : target.InnerText;
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
                value = Utils.Metadata.GetDate(value);
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
            using (ZipArchive zip = ZipFile.Open(FilePath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry origOpf = zip.Entries.FirstOrDefault(x => x.Name.EndsWith(".opf"));
                if (origOpf == null) throw new Exception("Metadata file content.opf not found, epub may be corrupt");

                XmlDocument OpfDocument = new XmlDocument();
                using (Stream s = origOpf.Open()) { OpfDocument.Load(s); }

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(OpfDocument.NameTable);
                nsmgr.AddNamespace("p", OpfDocument.DocumentElement.NamespaceURI);

                XmlNode metadataNode = OpfDocument.SelectSingleNode("//p:package/p:metadata", nsmgr);

                nsmgr.AddNamespace("dc", metadataNode.GetNamespaceOfPrefix("dc"));
                nsmgr.AddNamespace("opf", metadataNode.GetNamespaceOfPrefix("opf"));

                metadataNode.SelectSingleNode("dc:title", nsmgr).InnerText = Title;

                XmlNode authorNode = metadataNode.SelectSingleNode("dc:creator", nsmgr);
                authorNode.InnerText = Author;
                XmlElement an = (XmlElement)authorNode;
                an.SetAttribute("file-as", nsmgr.LookupNamespace("opf"), Utils.Metadata.SortAuthor(Author));

                metadataNode.SelectSingleNode("dc:publisher", nsmgr).InnerText = Publisher;
                metadataNode.SelectSingleNode("dc:date", nsmgr).InnerText = PubDate;

                XmlNode isbnNode = metadataNode.SelectSingleNode("dc:identifier[@opf:scheme='ISBN']", nsmgr);
                if (isbnNode == null) {
                    XmlElement node = OpfDocument.CreateElement("dc:identifier");
                    node.SetAttribute("opf:scheme", "ISBN");
                    metadataNode.AppendChild(node);
                    isbnNode = node;
                }
                isbnNode.InnerText = ISBN.ToString();

                origOpf.Delete();
                ZipArchiveEntry newOpf = zip.CreateEntry(origOpf.FullName);
                using (StreamWriter writer = new StreamWriter(newOpf.Open()))
                {
                    writer.Write(OpfDocument.OuterXml);
                }

                // toc.ncx is not required in Epub 3 so it can be safely ignored if it doesn't exist
                ZipArchiveEntry origTocNcx = zip.Entries.FirstOrDefault(x => x.Name == "toc.ncx");
                if (origTocNcx == null) return;

                XmlDocument tocXml = new XmlDocument();
                using (Stream s = origTocNcx.Open()) { tocXml.Load(s); }

                nsmgr.AddNamespace("n", tocXml.NamespaceURI);

                try
                {
                    tocXml.SelectSingleNode("//n:ncx/n:docTitle/n:text", nsmgr).InnerText = Author;
                }
                catch
                {
                    // Just bail if we can't write to the node.                   
                    return;
                }
                origTocNcx.Delete();
                ZipArchiveEntry newTocNcx = zip.CreateEntry(origTocNcx.FullName);
                using (StreamWriter writer = new StreamWriter(newTocNcx.Open()))
                {
                    writer.Write(tocXml.OuterXml);
                }

            }
        }
        #endregion
    }
}