using System;
using System.Collections.Generic;
using ExtensionMethods;
using System.IO;
using HtmlAgilityPack;
using System.Linq;

namespace Formats.Mobi
{
    public class Builder
    {
        private const int postHeaderPadding = 0x400;

        private static readonly byte[] nullTwo = new byte[2];
        private static readonly byte[] nullFour = new byte[4];

        readonly IBook Donor;
        readonly string OutputPath;
        (string, int)[] Chapters; // (title, byteoffset) in encoded html

        private Headers.PDBHeader PDB = new Headers.PDBHeader();
        private Headers.PalmDOCHeader PDH = new Headers.PalmDOCHeader();
        private Headers.MobiHeader MobiHeader = new Headers.MobiHeader();
        private Headers.EXTHHeader EXTH = new Headers.EXTHHeader();

        private List<ushort> idxtOffsets = new List<ushort>();

        List<byte[]> cncxBuffer = new List<byte[]>();
        List<byte> cncxLabelBuffer = new List<byte>();

        private List<byte[]> records = new List<byte[]>();


        public Builder(IBook donor, string outputPath)
        {
            Donor = donor ?? throw new ArgumentException("Input book cannot be null");
            OutputPath = outputPath;
        }


        public void Write()
        {
            (byte[] textBytes, (string, int)[] c) = ProcessHtml(Donor.TextContent());
            Chapters = c;

            // Make logical toc
            GenerateCNCX();

            // Split text records
            ushort textRecordCount = 0;
            for (int i = 0; i < textBytes.Length; i += 4096)
            {
                int len = Math.Min(4096, textBytes.Length - i);
                records.Add(textBytes.SubArray(i, len));
                textRecordCount++;
            }

            uint firstNonBookRecord = (uint)records.Count + 1;
            uint indxRecord = (uint)records.Count + 1;
            records.AddRange(IndxRecords());

            byte[][] images = Donor.Images();
            uint firstImageRecord = (images.Length == 0) ? uint.MaxValue: (uint)records.Count + 1;
            records.AddRange(Donor.Images());
            ushort lastContentRecord = (ushort)(records.Count);

            records.Add(FLISRecord);
            uint flisRecord = (uint)records.Count;
            records.Add(FCISRecord((uint)textBytes.Length));
            uint fcisRecord = (uint)records.Count;
            records.Add(EOFRecord);

            // Build headers backward to know lengths
            FillEXTHHeader();

            MobiHeader.FillDefault();
            MobiHeader.fullTitleOffset = MobiHeader.length + (uint)EXTH.length; //mobi + exth
            MobiHeader.firstNonBookRecord = firstNonBookRecord;
            MobiHeader.firstImageRecord = firstImageRecord;
            MobiHeader.lastContentRecord = lastContentRecord;
            MobiHeader.indxRecord = indxRecord;
            MobiHeader.fullTitle = Donor.Title;
            MobiHeader.flisRecord = flisRecord;
            MobiHeader.fcisRecord = fcisRecord;

            PDH.FillDefault();
            PDH.textLength = (uint)textBytes.Length;
            PDH.textRecordCount = textRecordCount;

            PDB.FillDefault();
            PDB.title = Donor.Title;
            PDB.recordCount = (ushort)records.Count;
            PDB.records = CalcRecordOffsets();

            using (FileStream file = new FileStream(OutputPath, FileMode.CreateNew))
            using (BinaryWriter writer = new BinaryWriter(file))
            {
                PDB.Write(writer);
                PDH.Write(writer, false);
                MobiHeader.Write(writer, false);
                EXTH.Write(writer, false);
                writer.Write(Donor.Title.Encode());
                writer.BaseStream.Seek(postHeaderPadding - Donor.Title.Length, SeekOrigin.Current);
                foreach (byte[] record in records)
                {
                    writer.Write(record);
                }
            }

            Book book = new Book(OutputPath);
            book.PDBHeader.Print();
            book.PalmDOCHeader.Print();
            book.MobiHeader.Print();
            book.EXTHHeader.Print();

            return;
        }

        #region Headers
        private uint[] CalcRecordOffsets()
        {
            List<uint> offsets = new List<uint>();

            uint currentPosition = (uint)PDB.TotalLength;
            offsets.Add(currentPosition); // start of PalmDoc

            currentPosition += 0x10 + MobiHeader.length + (uint)EXTH.length + postHeaderPadding;
            for (int i = 0; i < records.Count; i++)
            {
                offsets.Add(currentPosition);
                currentPosition += (uint)records[i].Length;
            }
            return offsets.ToArray();
        }

        /// <summary>
        /// https://wiki.mobileread.com/wiki/MOBI#EXTH_Header
        /// </summary>
        /// <returns></returns>
        private void FillEXTHHeader()
        {
            EXTH.identifier = "EXTH".Encode();
            EXTH.Set(Headers.EXTHRecordID.Author, Donor.Author.Encode());
            EXTH.Set(Headers.EXTHRecordID.Publisher, Donor.Publisher.Encode());
            EXTH.Set(Headers.EXTHRecordID.Description, Donor.Description.Encode());
            EXTH.Set(Headers.EXTHRecordID.ISBN, Donor.ISBN.ToString().Encode());
            EXTH.Set(Headers.EXTHRecordID.Subject, string.Join(", ", Donor.Subject).Encode());
            EXTH.Set(Headers.EXTHRecordID.PublishDate, Donor.PubDate.Encode());
            EXTH.Set(Headers.EXTHRecordID.Contributor, "Lignum".Encode());
            EXTH.Set(Headers.EXTHRecordID.Rights, Donor.Rights.Encode());
            EXTH.Set(Headers.EXTHRecordID.Creator, "Lignum".Encode());
            EXTH.Set(Headers.EXTHRecordID.Language, Donor.Language.Encode());
            EXTH.Set(Headers.EXTHRecordID.CDEType, "EBOK".Encode());
            EXTH.Set(Headers.EXTHRecordID.Source, "Lignum".Encode());

        }

        #endregion

        #region HtmlProcessing
        private (byte[], (string, int)[]) ProcessHtml(string html)
        {

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            StripStyle(doc);
            FixImageRecIndexes(doc);

            (string, int)[] tocData = FixLinks(doc, true);

            string decodedText = doc.DocumentNode.OuterHtml;

            byte[] textBytes = decodedText.Encode();

            return (textBytes, tocData);
        }

        /// <summary>
        /// Changes img src to recindex
        /// </summary>
        private void FixImageRecIndexes(HtmlDocument html)
        {
            HtmlNodeCollection imgs = html.DocumentNode.SelectNodes("//img");
            if (imgs == null) return;
            foreach (HtmlNode img in imgs)
            {
                string src = img.Attributes["src"].Value;
                if (src != null)
                {
                    img.SetAttributeValue("recindex", src);
                }
            }
        }

        /// <summary>
        /// Changes a href to filepos and adds TOC to end of document
        /// </summary>
        private (string, int)[] FixLinks(HtmlDocument html, bool addTOC)
        {
            HtmlNode tocReference;

            if (addTOC && html.DocumentNode.SelectSingleNode("//html/head/guide/reference[@type='toc']") == null)
            {
                HtmlNode guide = HtmlNode.CreateNode("<guide></guide>");
                tocReference = HtmlNode.CreateNode("<reference title='Table of Contents' type='toc' filepos='0000000000'/>");
                HtmlNode head = html.DocumentNode.SelectSingleNode("//html/head");
                if (head == null)
                {
                    throw new Exception("Unable to find <head> element in html");
                }

                guide.ChildNodes.Append(tocReference);
                head.ChildNodes.Prepend(guide);
            };

            // Give all anchors filepos property then reload html to get correct streampositions
            HtmlNodeCollection anchors = html.DocumentNode.SelectNodes("//a");
            if (anchors == null) return new (string, int)[0];
            foreach (HtmlNode a in anchors)
            {
                if (a.Attributes["href"] != null)
                {
                    a.SetAttributeValue("filepos", 0.ToString("D10"));
                }
            }
            html.LoadHtml(html.DocumentNode.OuterHtml);

            // Get all anchors again and match them to a targetnode
            anchors = html.DocumentNode.SelectNodes("//a");
            if (anchors == null) return new (string, int)[0];
            List<(string, HtmlNode)> targetNodes = new List<(string, HtmlNode)>();
            foreach (HtmlNode a in anchors)
            {
                HtmlAttribute href = a.Attributes["href"];
                if (href == null) continue;
                HtmlNode target = html.DocumentNode.SelectSingleNode($"//*[@id='{href.Value.Substring(1)}']");
                if (target == null) continue;
                if (!targetNodes.Any(x => x.Item2 == target))
                {
                    targetNodes.Add((a.InnerText, target));
                }
                a.SetAttributeValue("filepos", target.BytePosition().ToString("D10"));
            }

            if (addTOC)
            {
                html.DocumentNode.SelectSingleNode("//html/body").InnerHtml += BuildHtmlTOC(targetNodes);
                html.LoadHtml(html.DocumentNode.OuterHtml);
                tocReference = html.DocumentNode.SelectSingleNode("//html/head/guide/reference[@type='toc']");
                tocReference.SetAttributeValue("filepos", html.DocumentNode.SelectSingleNode("//p[@id='toc']").BytePosition().ToString("D10"));
            }

            List<(string, int)> tocData = new List<(string, int)>();
            foreach ((string, HtmlNode) target in targetNodes)
            {
                tocData.Add((target.Item1, target.Item2.BytePosition()));
            }
            tocData.Add(("EOF", html.DocumentNode.OuterHtml.Encode().Length));

            return tocData.ToArray();
        }

        private void StripStyle(HtmlDocument html)
        {
            HtmlNode style = html.DocumentNode.SelectSingleNode("//html/head/style");
            if (style == null) return;
            style.Remove();

        }

        #endregion

        #region INDX table/metadata
        /// <summary>
        /// We are putting every chapter is as one layer at zero depth. Want to fight about it?
        /// </summary>
        /// <param name="tocData"></param>
        private void GenerateCNCX()
        {
            List<byte> cncxEntry = new List<byte>();
            for (var i = 0; i < Chapters.Length; i++)
            {
                cncxEntry.Clear();

                (string chapterName, int chapterOffset) = Chapters[i];
                if (chapterName == "EOF") break;

                int chapterLength = Chapters[i + 1].Item2 - chapterOffset;

                idxtOffsets.Add((ushort)(Records.INDX.indxLength + cncxBuffer.TotalLength()));

                byte[] cncxId = i.ToString("D3").Encode();
                byte[] vliOffset = Utils.Mobi.EncVarLengthInt((uint)chapterOffset);
                byte[] vliLen = Utils.Mobi.EncVarLengthInt((uint)chapterLength);
                byte[] vliNameOffset = Utils.Mobi.EncVarLengthInt((uint)cncxLabelBuffer.Count);
                byte[] vliNameLen = Utils.Mobi.EncVarLengthInt((uint)chapterName.Encode().Length);

                cncxEntry.Add((byte)cncxId.Length);    // id length
                cncxEntry.AddRange(cncxId);            // id
                cncxEntry.Add(0x0f);                   // control byte
                cncxEntry.AddRange(vliOffset);         // encoded html position
                cncxEntry.AddRange(vliLen);            // length of encoded chapter
                cncxEntry.AddRange(vliNameOffset);     // offset of chapter name in nametable
                cncxEntry.AddRange(Utils.Mobi.EncVarLengthInt(0)); // Depth -- always 0.

                cncxBuffer.Add(cncxEntry.ToArray());

                cncxLabelBuffer.AddRange(vliNameLen);
                cncxLabelBuffer.AddRange(chapterName.Encode());
            }
        }

        /// <summary>
        /// 
        /// The tag table entries are multiple of 4 bytes. 
        /// [0] tag number, 
        /// [1] number of values,
        /// [2] bit mask
        /// [3] end of the control byte.
        /// If the fourth byte is 0x01, all other bytes of the entry are zero.
        /// 
        /// This particular pre-built entry is created from known table entries
        /// for a single chapter with no children. Code contains descriptions.
        /// 
        /// ControlByte for this entry type is: 0x0f (15)
        /// 
        /// https://wiki.mobileread.com/wiki/MOBI#TAGX_section
        /// </summary>
        private static readonly byte[][] tagXEntry = new byte[][]{
            new byte[]{1,1,1,0}, // Position
            new byte[]{2,1,2,0}, // Length
            new byte[]{3,1,4,0}, // Name Offset
            new byte[]{4,1,8,0}, // Depth Level
            new byte[]{0,0,0,1}  // End of Entry
        };

        /// <summary>
        /// Makes two INDX records
        /// First a metadata record with TAGX information
        /// Second a record with TOC information eg chapter names and offsets
        /// </summary>
        /// <returns></returns>
        private byte[][] IndxRecords()
        {
            byte[][] records = new byte[3][];
            records[0] = metaINDX();
            records[1] = dataINDX();
            records[2] = cncxLabelBuffer.ToArray();
            return records;
        }

        private byte[] metaINDX()
        {
            // Build tagx table
            List<byte> tagx = new List<byte>();
            tagx.AddRange("TAGX".Encode());                                         // magic
            tagx.AddRange(Utils.BigEndian.GetBytes((tagXEntry.Length * 4) + 12));   // total length of tagx
            tagx.AddRange(Utils.BigEndian.GetBytes(1));                             // control byte count -- always 1
            foreach (byte[] tag in tagXEntry)                                       // tagx table entry
            {
                tagx.AddRange(tag);
            }

            // Pad between tagx and idxt
            byte[] Rec = cncxBuffer.Last();
            Rec = Rec.SubArray(0, Rec[0] + 1);
            int Padding = (Rec.Length + 2) % 4;

            // Make indx
            Records.INDX indx = new Records.INDX();
            indx.type = 2;                          // inflection
            indx.recordCount = 1;                   // num of indx data records
            indx.recordEntryCount = (uint)Chapters.Length;
            indx.idxtOffset = (uint)(indx.length + tagx.Count + (Rec.Length + 2 + Padding));

            // Combine
            List<byte> record = new List<byte>();
            record.AddRange(indx.Dump());
            record.AddRange(tagx);
            record.AddRange(Rec);
            record.AddRange(Utils.BigEndian.GetBytes((ushort)idxtOffsets.Count));
            record.AddRange(new byte[Padding]);
            record.AddRange("IDXT".Encode());
            record.AddRange(Utils.BigEndian.GetBytes((ushort)(indx.length + tagx.Count)));
            record.AddRange(new byte[2]);

            return record.ToArray();
        }

        private byte[] dataINDX()
        {
            List<byte> record = new List<byte>();

            Records.INDX indx = new Records.INDX();
            indx.type = 0;                              // normal
            indx.unused = new byte[] { 0, 0, 0, 1 };    // this should be one with type=0 because reasons
            indx.encoding = 0xFFFFFFFF;
            indx.unused2 = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            indx.idxtOffset = (uint)(indx.length + cncxBuffer.Count);
            indx.recordCount = (uint)idxtOffsets.Count;

            record.AddRange(indx.Dump());
            foreach (byte[] rec in cncxBuffer)
            {
                record.AddRange(rec);
            }

            record.AddRange("IDXT".Encode());
            foreach (ushort offset in idxtOffsets) // this isn't right
            {
                record.AddRange(Utils.BigEndian.GetBytes(offset));
            }
            record.AddRange(new byte[(idxtOffsets.Count + 4) % 4]);
            
            return record.ToArray();
        }

        #endregion

        private string BuildHtmlTOC(List<(string, HtmlNode)> targets)
        {
            string[] parts = new string[targets.Count + 2];
            parts[0] = "<mbp:pagebreak/><p id='toc'>Table of Contents</p>";
            parts[parts.Length - 1] = "<mbp:pagebreak/>";

            string linkTemplate = "<blockquote><a filepos='{0}'>{1}</a></blockquote>";

            for (int i = 0; i < targets.Count; i++)
            {
                parts[i + 1] = string.Format(linkTemplate, targets[i].Item2.BytePosition().ToString("D10"), targets[i].Item1);
            }

            return string.Join("", parts);
        }

        private readonly byte[] FLISRecord = new byte[] { 0x46, 0x4C, 0x49, 0x53,
                                                    0x00, 0x00, 0x00, 0x08,
                                                    0x00, 0x41,
                                                    0x00, 0x00,
                                                    0x00, 0x00, 0x00, 0x00,
                                                    0xFF, 0xFF, 0xFF, 0xFF,
                                                    0x00, 0x01,
                                                    0x00, 0x03,
                                                    0x00, 0x00, 0x00, 0x03,
                                                    0x00, 0x00, 0x00, 0x01,
                                                    0xFF, 0xFF, 0xFF, 0xFF
                                                    };
        private byte[] FCISRecord(uint textLength)
        {
            byte[] rec = new byte[]{ 0x46, 0x43, 0x49, 0x53,
                                     0x00, 0x00, 0x00, 0x14,
                                     0x00, 0x00, 0x00, 0x10,
                                     0x00, 0x00, 0x00, 0x01,
                                     0x00, 0x00, 0x00, 0x00,
                                     0xFF, 0xFF, 0xFF, 0xFF, // this gets replaced by textLength
                                     0x00, 0x00, 0x00, 0x00,
                                     0x00, 0x00, 0x00, 0x20,
                                     0x00, 0x00, 0x00, 0x08,
                                     0x00, 0x01,
                                     0x00, 0x01,
                                     0x00, 0x00, 0x00, 0x00
                                    };

            byte[] tl = Utils.BigEndian.GetBytes(textLength);

            for (int i = 0; i < 4; i++)
            {
                rec[20 + i] = tl[i];
            }

            return rec;
        }
        private readonly byte[] EOFRecord = new byte[] { 0xe9, 0x8e, 0x0d, 0x0a };

    }
}
