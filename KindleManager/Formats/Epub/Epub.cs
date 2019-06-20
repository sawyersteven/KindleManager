﻿using ExtensionMethods;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
        public override string TextContent()
        {
            // todo add option to remove nodes with no innerhtml?
            // But what if an element exists just to have a border as a divider?
            HtmlDocument combinedText = new HtmlDocument();
            combinedText.LoadHtml(Resources.HtmlTemplate);

            XmlDocument tocXml = new XmlDocument();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(tocXml.NameTable);
            nsmgr.AddNamespace("rt", "http://www.daisy.org/z3986/2005/ncx/");

            using (var zip = ZipFile.Open(FilePath, ZipArchiveMode.Read))
            {
                ZipArchiveEntry toc = zip.Entries.FirstOrDefault(x => x.Name.EndsWith(".ncx"));
                if (toc == null) throw new Exception("TOC file not found, epub may be corrupt");
                using (Stream s = toc.Open()) { tocXml.Load(s); }

                string[] orderedDocumentNames = OrderedDocumentNames(zip);

                Dictionary<string, HtmlDocument> documents = LoadDocuments(zip, orderedDocumentNames);

                FixLinks(NavPoints(tocXml, nsmgr), documents);

                string[] docTexts = new string[orderedDocumentNames.Length];
                for (int i = 0; i < orderedDocumentNames.Length; i++)
                {
                    docTexts[i] = documents[orderedDocumentNames[i]].DocumentNode.InnerHtml;
                }

                combinedText.DocumentNode.SelectSingleNode("//body").InnerHtml = MergeDocuments(orderedDocumentNames, documents);

                // Add css
                ZipArchiveEntry css = zip.Entries.FirstOrDefault(x => x.Name.EndsWith(".css"));
                if (css != null)
                {
                    string stylesheet;
                    using (Stream s = css.Open())
                    using (StreamReader reader = new StreamReader(s))
                    {
                        stylesheet = reader.ReadToEnd();
                    }
                    combinedText.DocumentNode.SelectSingleNode("//head/style").InnerHtml += stylesheet;
                }
            }
            return SerializeImgLinks(combinedText);
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
        /// Makes list of document names in order of OPF's spine
        /// </summary>
        private string[] OrderedDocumentNames(ZipArchive zip)
        {
            XmlDocument opfXml = new XmlDocument();
            ZipArchiveEntry opf = zip.Entries.FirstOrDefault(x => x.Name.EndsWith(".opf"));
            if (opf == null) throw new Exception("OPF file not found, epub may be corrupt");
            using (Stream s = opf.Open()) { opfXml.Load(s); }
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(opfXml.NameTable);
            nsmgr.AddNamespace("rt", opfXml.DocumentElement.NamespaceURI);

            List<string> docNames = new List<string>();
            foreach (XmlNode itemref in opfXml.SelectNodes("//rt:spine/rt:itemref", nsmgr))
            {
                XmlAttribute idref = itemref.Attributes["idref"];
                if (idref == null)
                {
                    Console.WriteLine("Itemref entry does not have 'idref' attribute, html parsing may be incorrect");
                    continue;
                }
                XmlNode item = opfXml.SelectSingleNode($"//rt:manifest/rt:item[@id='{idref.Value}']", nsmgr);
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
                docNames.Add(href.Value);
            }
            return docNames.ToArray();
        }

        /// <summary>
        /// Reads documents' contents as string into dict {filename: contents}
        /// </summary>
        private Dictionary<string, HtmlDocument> LoadDocuments(ZipArchive zip, string[] docNames)
        {
            Dictionary<string, HtmlDocument> documents = new Dictionary<string, HtmlDocument>();
            foreach (string docname in docNames)
            {
                if (documents.ContainsKey(docname)) continue;

                ZipArchiveEntry html = zip.Entries.FirstOrDefault(x => x.FullName.EndsWith(docname));
                if (html == null) throw new FileFormatException($"Content file {docname} could not be found.");

                HtmlDocument doc = new HtmlDocument();
                using (Stream s = html.Open()) { doc.Load(s, System.Text.Encoding.UTF8); }
                documents.Add(docname, doc);
            }

            return documents;
        }

        /// <summary>
        /// Combines documents' <body> contents into one document
        /// </summary>
        private static string MergeDocuments(string[] orderedDocumentNames, Dictionary<string, HtmlDocument> documents)
        {
            string[] docTexts = new string[orderedDocumentNames.Length];
            for (int i = 0; i < orderedDocumentNames.Length; i++)
            {
                HtmlNode body = documents[orderedDocumentNames[i]].DocumentNode.SelectSingleNode("//html/body");
                if (body == null) continue;
                docTexts[i] = body.InnerHtml;
            }
            return string.Join("<mbp:pagebreak/>", docTexts);
        }

        /// <summary>
        /// Fixes cross-document links
        /// 
        /// Epubs can have links that point to other documents, ie href="part2.html#chapternine"
        /// Because of this, a single book can have multiple nodes with the same ids but in different documents
        /// All anchor nodes are collected from every document in the book. Then the document that anchor
        ///     refers to has the id replaced with a number that is then incremented.
        /// 
        /// </summary>
        /// <param name="navPoints">(srcdoc#id, label) in order of toc.ncx's playorder</param>
        /// <param name="documents">Dict<string docname, HtmlDocument contents> of all html docs pointed to in toc.ncx</param>
        /// <returns></returns>
        private void FixLinks((string, string)[] navPoints, Dictionary<string, HtmlDocument> documents)
        {
            char[] split = new char[] { '#' };
            string[] parts;

            List<HtmlNode> bookAnchors = new List<HtmlNode>();
            foreach (HtmlDocument doc in documents.Values)
            {
                HtmlNodeCollection anchors = doc.DocumentNode.SelectNodes("//a");
                if (anchors != null)
                {
                    bookAnchors.AddRange(anchors.Where(x => x.Attributes["href"] != null));
                }
            }

            int counter = 1;
            foreach ((string url, string label) in navPoints)
            {
                string newId = counter.ToString("D10");
                string targetOldId = null;

                HtmlNode target;

                parts = url.Split(split, 2);
                HtmlDocument doc = documents[parts[0]];
                if (parts.Length == 1)
                {   // url points to document root, use first child node
                    target = doc.DocumentNode.SelectSingleNode("//html/body/*");
                }
                else
                {
                    target = doc.DocumentNode.SelectSingleNode($"//*[@id='{parts[1]}']");
                }

                target.SetAttributeValue("toclabel", label);

                HtmlAttribute id = target.Attributes["id"];
                if (id != null)
                {
                    targetOldId = id.Value;
                }

                target.SetAttributeValue("id", newId);
                counter++;

                if (targetOldId == null) continue;

                foreach (HtmlNode a in bookAnchors)
                {
                    string href = a.Attributes["href"].Value;
                    parts = href.Split(split, 2);

                    if (parts.Length == 1) continue;

                    if (parts[1] == targetOldId) a.SetAttributeValue("href", "#" + newId);

                }
            }
        }

        private string SerializeImgLinks(HtmlDocument html)
        {
            Dictionary<string, string> imageSubs = new Dictionary<string, string>();
            for (int i = 0; i < ImageNames.Length; i++)
            {
                imageSubs.Add(ImageNames[i], $"{(i + 1).ToString("D5")}.jpg");
            }

            HtmlNodeCollection imgNodes = html.DocumentNode.SelectNodes("//img");
            if (imgNodes != null)
            {
                foreach (HtmlNode img in imgNodes)
                {
                    if (imageSubs.TryGetValue(img.Attributes["src"].Value, out string newSource))
                    {
                        img.SetAttributeValue("src", newSource);
                    }
                }
            }

            return html.DocumentNode.OuterHtml;
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