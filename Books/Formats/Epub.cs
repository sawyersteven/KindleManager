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

            XmlDocument metadataXml = new XmlDocument();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(metadataXml.NameTable);

            using (var zip = ZipFile.Open(FilePath, ZipArchiveMode.Read))
            {
                ZipArchiveEntry metadataOpf = zip.Entries.FirstOrDefault(x => x.Name.EndsWith(".opf"));
                if (metadataOpf == null) throw new Exception("OPF metadata file not found, epub may be corrupt");
                using (Stream s = metadataOpf.Open()) { metadataXml.Load(s); }
            }

            nsmgr.AddNamespace("p", metadataXml.DocumentElement.NamespaceURI);

            XmlNode metadataNode = metadataXml.SelectSingleNode("//p:package/p:metadata", nsmgr);
            nsmgr.AddNamespace("dc", metadataNode.GetNamespaceOfPrefix("dc"));
            nsmgr.AddNamespace("opf", metadataNode.GetNamespaceOfPrefix("opf"));

            Title = ReadNode(metadataNode, "dc:title", nsmgr);
            Language = ReadNode(metadataNode, "dc:language", nsmgr);
            ulong.TryParse(ReadNode(metadataNode, "dc:identifier[@opf:scheme='ISBN']", nsmgr), out ulong id);
            ISBN = id;

            Author = ReadNode(metadataNode, "dc:creator", nsmgr);
            Contributor = ReadNode(metadataNode, "dc:contributor", nsmgr);
            Publisher = ReadNode(metadataNode, "dc:publisher", nsmgr);

            XmlNodeList subjects = metadataNode.SelectNodes("dc:subject", nsmgr);
            Subject = new string[subjects.Count];
            for (int i = 0; i < subjects.Count; i++)
            {
                Subject[i] = subjects[i].InnerText;
            }
            PubDate = ReadNode(metadataNode, "dc:date", nsmgr);
            Rights = ReadNode(metadataNode, "dc:rights", nsmgr);

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
                Type: {Format}
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
        public string FilePath { get; set; }
        public string Format { get => "EPUB"; }

        private string _Title;
        public string Title {
            get => _Title;
            set
            {
                _Title = value;
            }
        }

        private string _Language;
        public string Language {
            get => _Language;
            set
            {
                _Language = value;
            }
        }

        private ulong _ISBN;
        public ulong ISBN
        {
            get => _ISBN;
            set
            {
                _ISBN = value;
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

        private string _Contributor;
        public string Contributor
        {
            get => _Contributor;
            set
            {
                _Contributor = value;
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

        private string[] _Subject;
        public string[] Subject
        {
            get => _Subject;
            set
            {
                _Subject = value;
            }
        }

        private string _Description;
        public string Description
        {
            get => _Description;
            set
            {
                _Description = value;
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

        private string _Rights;
        public string Rights
        {
            get => _Rights;
            set
            {
                _Rights = value;
            }
        }


        // local db only, not parsed
        public int Id { get; set; }

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

        public string TextContent()
        {
            throw new NotImplementedException();
        }

        public byte[][] Images()
        {
            throw new NotImplementedException();
        }

        public void WriteMetadata()
        {
            WriteOPF();
            WriteTOC();
        }

        private void WriteOPF()
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
            }
        }
        private void WriteTOC() {
            using (ZipArchive zip = ZipFile.Open(FilePath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry origTocNcx = zip.Entries.FirstOrDefault(x => x.Name == "toc.ncx");
                XmlDocument tocXml = new XmlDocument();
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(tocXml.NameTable);

                if (origTocNcx == null)
                {

                } else {
                    using (Stream s = origTocNcx.Open()) { tocXml.Load(s); }
                    nsmgr.AddNamespace("n", tocXml.NamespaceURI);
                }

                try
                {
                    tocXml.SelectSingleNode("//n:ncx/n:docTitle/n:text", nsmgr).InnerText = Author;
                    origTocNcx.Delete();
                    ZipArchiveEntry newTocNcx = zip.CreateEntry(origTocNcx.FullName);
                    using (StreamWriter writer = new StreamWriter(newTocNcx.Open()))
                    {
                        writer.Write(tocXml.OuterXml);
                    }
                }
                catch{
                    return;
                }
            }
        }
        #endregion
    }
}