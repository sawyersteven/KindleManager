using ExtensionMethods;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

namespace Formats.Epub
{
    public class Book : BookBase
    {
        public Book(string filepath)
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
            Description = ReadNode(metadataNode, "dc:description", nsmgr);

            XmlNodeList subjects = metadataNode.SelectNodes("dc:subject", nsmgr);
            Subject = new string[subjects.Count];
            for (int i = 0; i < subjects.Count; i++)
            {
                Subject[i] = subjects[i].InnerText;
            }
            PubDate = ReadNode(metadataNode, "dc:date", nsmgr);
            Rights = ReadNode(metadataNode, "dc:rights", nsmgr);

            List<string> imageNames = new List<string>();
            foreach (XmlNode img in metadataXml.SelectNodes("//p:package/p:manifest/p:item[@media-type='image/jpeg']", nsmgr))
            {
                imageNames.Add(img.Attributes["href"].Value);
            }

            ImageNames = imageNames.ToArray();
        }

        /// <summary>
        /// Reads text from node. Returns empty string if node doesn't exist.
        /// </summary>
        private string ReadNode(XmlNode node, string xpath, XmlNamespaceManager nsmgr)
        {
            XmlNode target = node.SelectSingleNode(xpath, nsmgr);
            return (target == null) ? "" : target.InnerText;
        }

        private readonly string[] ImageNames;

        #region IBook overrides
        public override Tuple<string, HtmlDocument>[] TextContent()
        {
            Tuple<string, string, HtmlDocument>[] chapters;

            XmlDocument tocXml = new XmlDocument();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(tocXml.NameTable);
            nsmgr.AddNamespace("rt", "http://www.daisy.org/z3986/2005/ncx/");

            using (var zip = ZipFile.Open(FilePath, ZipArchiveMode.Read))
            {
                ZipArchiveEntry xmldoc = zip.Entries.FirstOrDefault(x => x.Name == "toc.ncx");
                if (xmldoc == null) throw new Exception("Could not find toc.ncx, epub may be corrupt.");
                using (Stream s = xmldoc.Open()) { tocXml.Load(s); }

                ValueTuple<string, string>[] orderedDocumentNames = OrderedDocumentNames(zip);
                // <chapter name, docname, contents>
                chapters = LoadDocuments(zip, orderedDocumentNames);
            }

            return FixLinks(NavPoints(tocXml, nsmgr), chapters);
        }

        #endregion

        #region html parsing
        /// <summary>
        /// Gets all navpoints from toc sorted by PlayOrder
        /// Returns array of tuples of (srcdocument#id, label)
        /// </summary>
        private (string, string)[] NavPoints(XmlDocument tocXml, XmlNamespaceManager nsmgr)
        {

            // Tuple of playOrder, src document, label
            List<(int, string, string)> navPoints = new List<(int, string, string)>();

            foreach (XmlNode nav in tocXml.SelectNodes("//rt:navPoint", nsmgr))
            {
                if (!int.TryParse(nav.Attributes["playOrder"].Value, out int playorder)) continue;

                XmlNode ctnt = nav.SelectSingleNode("rt:content", nsmgr);
                if (ctnt == null) continue;
                string src = ctnt.Attributes["src"].Value;
                if (src == "") continue;

                XmlNode lbl = nav.SelectSingleNode("rt:navLabel/rt:text", nsmgr);
                string label = "";
                if (lbl != null)
                {
                    label = lbl.InnerText;
                }
                navPoints.Add((playorder, src, label));
            }

            navPoints.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            (string, string)[] n = new (string, string)[navPoints.Count];
            for (int i = 0; i < navPoints.Count; i++)
            {
                n[i] = (navPoints[i].Item2, navPoints[i].Item3);
            }
            return n;
        }

        /// <summary>
        /// Makes list of <chapter name, chapter document>
        /// If name is null it is a separate document that belongs to the previous named chapter
        /// </summary>
        private ValueTuple<string, string>[] OrderedDocumentNames(ZipArchive zip)
        {
            XmlDocument opfXml = new XmlDocument();
            XmlDocument ncxXml = new XmlDocument();

            ZipArchiveEntry opf = zip.Entries.FirstOrDefault(x => x.Name.EndsWith(".opf"));
            if (opf == null) throw new Exception("OPF file not found, epub may be corrupt");
            using (Stream s = opf.Open()) { opfXml.Load(s); }

            ZipArchiveEntry ncx = zip.Entries.FirstOrDefault(x => x.Name.EndsWith(".ncx"));
            if (ncx == null) throw new Exception("NCX file not found, epub may be corrupt");
            using (Stream s = ncx.Open()) { ncxXml.Load(s); }

            XmlNamespaceManager opfNsmgr = new XmlNamespaceManager(opfXml.NameTable);
            opfNsmgr.AddNamespace("rt", opfXml.DocumentElement.NamespaceURI);

            XmlNamespaceManager ncxNsmgr = new XmlNamespaceManager(ncxXml.NameTable);
            ncxNsmgr.AddNamespace("rt", ncxXml.DocumentElement.NamespaceURI);

            List<ValueTuple<string, string>> docNames = new List<ValueTuple<string, string>>();
            XmlNodeList itemRefs = opfXml.SelectNodes("//rt:spine/rt:itemref", opfNsmgr);
            foreach (XmlNode itemref in itemRefs)
            {
                XmlAttribute idref = itemref.Attributes["idref"];
                if (idref == null)
                {
                    Console.WriteLine("Itemref entry does not have 'idref' attribute, html parsing may be incorrect");
                    continue;
                }
                XmlNode item = opfXml.SelectSingleNode($"//rt:manifest/rt:item[@id='{idref.Value}']", opfNsmgr);
                if (item == null)
                {
                    Console.WriteLine("Itemref points to an item that does not exist, html parsing may be incorrect");
                    continue;
                }
                XmlAttribute href = item.Attributes["href"];
                if (href == null)
                {
                    string itemid = item.Attributes["id"].Value;
                    Console.WriteLine($"Item entry {itemid} does not have 'href' attribute, html parsing may be incorrect");
                    continue;
                }

                string targetId = href.Value;

                string name = null;
                var node = ncxXml.SelectSingleNode($"//rt:ncx/rt:navMap/*/rt:content[@src='{targetId}']", ncxNsmgr);
                if (node != null)
                {
                    var navPoint = node.ParentNode;
                    var label = navPoint.SelectSingleNode("rt:navLabel", ncxNsmgr);
                    name = label?.InnerText;
                }

                docNames.Add(new ValueTuple<string, string>(name, Path.GetFileName(targetId)));
            }
            return docNames.ToArray();
        }

        /// <summary>
        /// Reads documents' into reference tuple of <chapter, document, contents>
        /// </summary>
        private Tuple<string, string, HtmlDocument>[] LoadDocuments(ZipArchive zip, ValueTuple<string, string>[] docNames)
        {
            List<Tuple<string, string, HtmlDocument>> documents = new List<Tuple<string, string, HtmlDocument>>();
            foreach ((string name, string document) in docNames)
            {
                if (documents.Any(x => x.Item2 == document)) continue;

                ZipArchiveEntry html = zip.Entries.FirstOrDefault(x => x.FullName.EndsWith(document));
                if (html == null) throw new FileFormatException($"Content file {document} could not be found.");

                HtmlDocument doc = new HtmlDocument();
                using (Stream s = html.Open())
                {
                    doc.LoadHtml(new StreamReader(s).ReadToEnd());
                }

                if (name == null)
                {
                    if (documents.Count == 0)
                    {
                        documents.Add(new Tuple<string, string, HtmlDocument>("Begin Reading", document, doc));
                    }
                    else
                    {
                        string contents = doc.DocumentNode.SelectSingleNode("//body").InnerHtml;
                        documents.Last().Item3.DocumentNode.SelectSingleNode("//body").InnerHtml += contents;

                    }
                }
                else
                {
                    documents.Add(new Tuple<string, string, HtmlDocument>(name, document, doc));
                }
            }
            return documents.ToArray();
        }

        /// <summary>
        /// Fixes cross-document links
        /// 
        /// Epubs can have links that point to other documents, ie href="part2.html#chapternine"
        /// Because of this, a single book can have multiple nodes with the same ids but in different documents
        /// All anchor nodes are collected from every document in the book. Then the document that anchor
        ///     refers to has the id replaced with a number that is then incremented.
        /// 
        /// Changes image links to sequential numbers based on Images() output;
        /// </summary>
        private Tuple<string, HtmlDocument>[] FixLinks((string, string)[] navPoints, Tuple<string, string, HtmlDocument>[] documents)
        {
            List<Tuple<string, HtmlDocument>> output = new List<Tuple<string, HtmlDocument>>();

            char[] split = new char[] { '#' };
            string[] parts;

            List<HtmlNode> bookAnchors = new List<HtmlNode>();
            foreach ((string chname, string docname, HtmlDocument doc) in documents)
            {
                HtmlNodeCollection anchors = doc.DocumentNode.SelectNodes("//a");
                if (anchors != null)
                {
                    bookAnchors.AddRange(anchors.Where(x => x.Attributes["href"] != null));
                }
            }

            Dictionary<string, string> imageSubs = new Dictionary<string, string>();
            int targetIdCounter = 1;
            foreach ((string url, string label) in navPoints)
            {
                string newId = targetIdCounter.ToString("D10");
                string targetOldId = null;

                HtmlNode target;
                parts = url.Split(split, 2);

                var m = documents.FirstOrDefault(x => x.Item2 == Path.GetFileName(parts[0]));
                if (m == null) continue;

                (string chname, string docname, HtmlDocument doc) = m;

                if (parts.Length == 1) // url points to document root, use first child node
                {
                    target = doc.DocumentNode.SelectSingleNode("//html/body/*");
                    target.SetAttributeValue("id", parts[0]);
                }
                else
                {
                    target = doc.DocumentNode.SelectSingleNode($"//*[@id='{parts[1]}']");
                }

                HtmlAttribute id = target.Attributes["id"];
                if (id != null)
                {
                    targetOldId = id.Value;
                }
                if (targetOldId == null) continue;

                target.SetAttributeValue("id", newId);
                targetIdCounter++;

                foreach (HtmlNode a in bookAnchors)
                {
                    string href = a.GetAttributeValue("href", null);
                    string wantedId = href.Split(split, 2).Last();
                    if (wantedId == targetOldId) a.SetAttributeValue("href", "#" + newId);
                }

                for (int i = 0; i < ImageNames.Length; i++)
                {
                    if (imageSubs.TryGetValue(ImageNames[i], out string _)) continue;
                    string k = Path.GetFileName(ImageNames[i]);
                    if (imageSubs.ContainsKey(k)) continue;
                    imageSubs.Add(k, $"{(i + 1).ToString("D5")}.jpg");
                }

                HtmlNodeCollection imgNodes = doc.DocumentNode.SelectNodes("//img");
                if (imgNodes != null)
                {
                    foreach (HtmlNode img in imgNodes)
                    {
                        if (imageSubs.TryGetValue(Path.GetFileName(img.Attributes["src"].Value), out string newSource))
                        {
                            img.SetAttributeValue("src", newSource);
                        }
                    }
                }
            }

            return documents.Select(x => new Tuple<string, HtmlDocument>(x.Item1, x.Item3)).ToArray();
        }

        public override byte[][] Images()
        {
            byte[][] images = new byte[ImageNames.Length][];
            using (var zip = ZipFile.Open(FilePath, ZipArchiveMode.Read))
            {
                for (int i = 0; i < ImageNames.Length; i++)
                {
                    ZipArchiveEntry img = zip.Entries.FirstOrDefault(x => x.FullName.EndsWith(ImageNames[i]));
                    if (img == null) continue;
                    using (Stream s = img.Open())
                    using (BinaryReader reader = new BinaryReader(s))
                    {
                        images[i] = reader.ReadAllBytes();
                    }
                }
            }
            return images;
        }

        public override byte[] StyleSheet()
        {
            StringBuilder stylesheet;
            using (var zip = ZipFile.Open(FilePath, ZipArchiveMode.Read))
            {
                ZipArchiveEntry[] sheets = zip.Entries.Where(x => x.Name.EndsWith(".css")).ToArray();

                stylesheet = new StringBuilder(sheets.Length);

                foreach (ZipArchiveEntry css in sheets)
                {
                    using (Stream s = css.Open())
                    using (StreamReader reader = new StreamReader(s))
                    {
                        stylesheet.Append(reader.ReadToEnd());
                    }
                }
            }
            return stylesheet.ToString().Encode();
        }

        public override void WriteMetadata()
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
                if (isbnNode == null)
                {
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
        private void WriteTOC()
        {
            using (ZipArchive zip = ZipFile.Open(FilePath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry origTocNcx = zip.Entries.FirstOrDefault(x => x.Name == "toc.ncx");
                XmlDocument tocXml = new XmlDocument();
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(tocXml.NameTable);

                if (origTocNcx == null)
                {

                }
                else
                {
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
                catch
                {
                    return;
                }
            }
        }
        #endregion
    }
}