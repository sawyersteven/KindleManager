using ExtensionMethods;
using HtmlAgilityPack;
using NCss;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EXTHRecordID = Formats.Mobi.Headers.EXTHKey;

namespace Formats.Mobi
{
    /// <summary>
    /// Mobi6 builder class
    /// </summary>
    public class Builder
    {
        private const int postHeaderPadding = 0x400;

        private readonly BookBase Donor;
        private readonly string OutputPath;
        (string, int)[] Chapters; // (title, byteoffset in encoded html)

        private readonly Headers.PDBHeader PDB = new Headers.PDBHeader();
        private readonly Headers.PalmDOCHeader PDH = new Headers.PalmDOCHeader();
        private readonly Headers.MobiHeader MobiHeader = new Headers.MobiHeader(Headers.MobiHeaderType.Mobi6);
        private readonly Headers.EXTHHeader EXTH = new Headers.EXTHHeader();

        private readonly List<ushort> idxtOffsets = new List<ushort>();

        private readonly List<byte[]> logicalTOCEntries = new List<byte[]>();
        private readonly List<byte> logicalTOCLabels = new List<byte>();

        private uint firstNonTextRecord;
        private uint firstImageRecord;
        private ushort lastContentRecord;
        private uint ncxIndxRecord;
        private uint flisRecord;
        private uint fcisRecord;
        private uint textLength;
        private ushort textRecordCount;

        public Builder(BookBase donor, string outputPath)
        {
            Donor = donor ?? throw new ArgumentException("Input book cannot be null");
            OutputPath = outputPath;
        }

        public BookBase Convert()
        {
            byte[] textBytes;
            (textBytes, Chapters) = ProcessHtml(Donor.TextContent());

            GenerateLogicalTOC();

            byte[][] records = MakeRecords(textBytes);

            FillEXTHHeader();

            MobiHeader.FillDefault();
            MobiHeader.firstNonTextRecord = firstNonTextRecord;
            MobiHeader.firstImageRecord = firstImageRecord;
            MobiHeader.lastContentRecord = lastContentRecord;
            MobiHeader.ncxIndxRecord = ncxIndxRecord;
            MobiHeader.fullTitle = Donor.Title;
            MobiHeader.flisRecord = flisRecord;
            MobiHeader.fcisRecord = fcisRecord;

            PDH.FillDefault();
            PDH.textLength = textLength;
            PDH.textRecordCount = textRecordCount;

            PDB.FillDefault();
            PDB.title = Donor.Title;
            PDB.recordCount = (ushort)records.Length;
            PDB.records = CalcRecordOffsets(records);

            MobiHeader.fullTitleOffset = 0x10 + (uint)PDB.TotalLength + MobiHeader.length + (uint)EXTH.length;

            using (FileStream file = new FileStream(OutputPath, FileMode.CreateNew))
            using (BinaryWriter writer = new BinaryWriter(file))
            {
                PDB.Write(writer);
                PDH.offset = (uint)writer.BaseStream.Position;
                PDH.Write(writer);
                MobiHeader.offset = (uint)writer.BaseStream.Position;
                MobiHeader.Write(writer);
                EXTH.offset = (uint)writer.BaseStream.Position;
                EXTH.Write(writer);
                MobiHeader.WriteTitle(writer);
                writer.BaseStream.Seek(postHeaderPadding - Donor.Title.Length, SeekOrigin.Current);
                foreach (byte[] record in records)
                {
                    writer.Write(record);
                }
            }

            try { return new Mobi.Book(OutputPath); }
            catch (Exception e)
            {
                File.Delete(OutputPath);
                throw e;
            }
        }

        private byte[][] MakeRecords(byte[] textBytes)
        {
            List<byte[]> records = new List<byte[]>();

            textLength = (uint)textBytes.Length;

            textRecordCount = 0;
            for (int i = 0; i < textBytes.Length; i += 4096)
            {
                int len = Math.Min(4096, textBytes.Length - i);
                records.Add(textBytes.SubArray(i, len));
                textRecordCount++;
            }

            firstNonTextRecord = (uint)records.Count + 1;
            ncxIndxRecord = (uint)records.Count + 1;
            records.AddRange(IndxRecords());

            byte[][] images = Donor.Images();
            firstImageRecord = (images.Length == 0) ? uint.MaxValue : (uint)records.Count + 1;
            records.AddRange(Donor.Images());
            lastContentRecord = (ushort)(records.Count);

            records.Add(FLISRecord);
            flisRecord = (uint)records.Count;
            records.Add(FCISRecord((uint)textBytes.Length));
            fcisRecord = (uint)records.Count;
            records.Add(EOFRecord);

            return records.ToArray();
        }

        #region Headers

        /// <summary>
        /// Calculates file/text record offsets for this.records
        /// </summary>
        private uint[] CalcRecordOffsets(byte[][] records)
        {
            List<uint> offsets = new List<uint>();

            uint currentPosition = (uint)PDB.TotalLength;
            offsets.Add(currentPosition);

            currentPosition += PDH.length + MobiHeader.length + (uint)EXTH.length + postHeaderPadding;
            for (int i = 0; i < records.Length; i++)
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
            EXTH.Set(EXTHRecordID.Author, Donor.Author.Encode());
            EXTH.Set(EXTHRecordID.Publisher, Donor.Publisher.Encode());
            EXTH.Set(EXTHRecordID.Description, Donor.Description.Encode());
            EXTH.Set(EXTHRecordID.ISBN, Donor.ISBN.ToString().Encode());
            EXTH.Set(EXTHRecordID.Subject, string.Join(", ", Donor.Subject).Encode());
            EXTH.Set(EXTHRecordID.PublishDate, Donor.PubDate.Encode());
            EXTH.Set(EXTHRecordID.Contributor, "KindleManager".Encode());
            EXTH.Set(EXTHRecordID.Rights, Donor.Rights.Encode());
            EXTH.Set(EXTHRecordID.Creator, "KindleManager".Encode());
            EXTH.Set(EXTHRecordID.Language, Donor.Language.Encode());
            EXTH.Set(EXTHRecordID.CDEType, "EBOK".Encode());
            EXTH.Set(EXTHRecordID.Source, "KindleManager".Encode());
        }

        #endregion

        #region HtmlProcessing
        /// <summary>
        /// Returns single html doc containing all chapters and toc data including
        /// an EOF entry pointing to the end of the readable contents.
        /// Changes anchors to mobi-style filepos links
        /// </summary>
        /// <param name="chapters"></param>
        /// <returns></returns>
        private (byte[], (string, int)[]) ProcessHtml(Tuple<string, HtmlDocument>[] chapters)
        {
            FixLinks(chapters);
            ApplyStyleToNodes(Donor.StyleSheet().Decode(), chapters);

            HtmlDocument combinedText = new HtmlDocument();
            combinedText.LoadHtml(Resources.HtmlTemplate);

            List<ValueTuple<string, int>> tocData = new List<ValueTuple<string, int>>();
            int tally = combinedText.DocumentNode.SelectSingleNode("//body").BytePosition();
            foreach ((string title, HtmlDocument chapter) in chapters)
            {
                tocData.Add((title, tally));
                string addedHtml = chapter.DocumentNode.SelectSingleNode("//body").InnerHtml;
                combinedText.DocumentNode.SelectSingleNode("//body").InnerHtml += addedHtml;

                tally += addedHtml.Encode().Length;
            }
            tocData.Add(("EOF", tally));

            ResolveFilePos(combinedText);

            return (combinedText.DocumentNode.OuterHtml.Encode(), tocData.ToArray());
        }

        private void ApplyStyleToNodes(string stylesheet, Tuple<string, HtmlDocument>[] chapters)
        {
            Stylesheet cssSheet = new CssParser().ParseSheet(stylesheet);

            List<string> possibleSelectors = new List<string>();
            foreach ((string name, HtmlDocument doc) in chapters)
            {
                foreach (HtmlNode node in doc.DocumentNode.Descendants())
                {
                    if (node.NodeType != HtmlNodeType.Element) continue;

                    possibleSelectors.Clear();
                    possibleSelectors.Add(node.Name);
                    if (node.Id != string.Empty)
                    {
                        possibleSelectors.Add($"{node.Name}#{node.Id}");
                        possibleSelectors.Add(node.Id);
                    }
                    foreach (string cls in node.GetClasses())
                    {
                        possibleSelectors.Add($"{node.Name}.{cls}");
                        possibleSelectors.Add($".{cls}");
                    }

                    ClassRule combinedRules = new ClassRule();
                    foreach (Rule r in cssSheet.Rules)
                    {
                        if (!(r is ClassRule rule)) continue;
                        if (rule.Selector is SelectorList selList && selList.Selectors.Any(x => possibleSelectors.Contains(x.OriginalToken)))
                        {
                            combinedRules.Properties.AddRange(rule.Properties);
                        }
                        else if (possibleSelectors.Contains(rule.Selector.ToString()))
                        {
                            combinedRules.Properties.AddRange(rule.Properties);
                        }
                    }

                    foreach (Property prop in combinedRules.Properties)
                    {
                        node.SetAttributeValue(prop.Name, prop.Values[0].OriginalToken);
                    }
                }
            }
        }

        /// <summary>
        /// Sets filepos on anchors to correct value
        /// </summary>
        /// <param name="doc"></param>
        private void ResolveFilePos(HtmlDocument doc)
        {
            HtmlNodeCollection anchors = doc.DocumentNode.SelectNodes("//a");
            if (anchors != null)
            {
                foreach (HtmlNode anchor in anchors)
                {
                    string href = anchor.GetAttributeValue("href", null);
                    if (href == null) continue;
                    HtmlNode target = doc.DocumentNode.SelectSingleNode($"//*[@id='{href}']");
                    if (target == null) continue;
                    anchor.SetAttributeValue("filepos", target.BytePosition().ToString("D10"));
                }
            }
        }

        /// <summary>
        /// Changes img src to recindex
        /// Changes anchors to mobi-style filepos links with all zeros ie filepos="0000000000"
        /// </summary>
        private void FixLinks(Tuple<string, HtmlDocument>[] chapters)
        {
            foreach ((string chname, HtmlDocument doc) in chapters)
            {
                HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//img");
                if (nodes != null)
                {
                    foreach (HtmlNode img in nodes)
                    {
                        string src = img.Attributes["src"].Value;
                        if (src != null) img.SetAttributeValue("recindex", Path.GetFileNameWithoutExtension(src));
                    }
                }
                nodes = doc.DocumentNode.SelectNodes("//a");
                if (nodes != null)
                {
                    foreach (HtmlNode a in nodes)
                    {
                        if (a.Attributes["href"] != null)
                        {
                            a.SetAttributeValue("filepos", "0000000000");
                        }
                    }
                }
            }
        }
        #endregion

        #region INDX table/metadata
        /// <summary>
        /// We are putting every chapter is as one layer at zero depth. Want to fight about it?
        /// </summary>
        /// <param name="tocData"></param>
        private void GenerateLogicalTOC()
        {
            List<byte> tocEntry = new List<byte>();
            for (var i = 0; i < Chapters.Length; i++)
            {
                tocEntry.Clear();

                (string chapterName, int chapterOffset) = Chapters[i];
                if (chapterName == "EOF") break;

                int chapterLength = Chapters[i + 1].Item2 - chapterOffset;

                idxtOffsets.Add((ushort)(Records.INDX.indxLength + logicalTOCEntries.TotalLength()));

                byte[] cncxId = i.ToString("D3").Encode();
                byte[] vliOffset = Utils.Mobi.EncodeVWI((uint)chapterOffset, true);
                byte[] vliLen = Utils.Mobi.EncodeVWI((uint)chapterLength, true);
                byte[] vliNameOffset = Utils.Mobi.EncodeVWI((uint)logicalTOCLabels.Count, true);
                byte[] vliNameLen = Utils.Mobi.EncodeVWI((uint)chapterName.Encode().Length, true);

                tocEntry.Add((byte)cncxId.Length);    // id length
                tocEntry.AddRange(cncxId);            // id
                tocEntry.Add(0x0f);                   // control byte
                tocEntry.AddRange(vliOffset);         // encoded html position
                tocEntry.AddRange(vliLen);            // length of encoded chapter
                tocEntry.AddRange(vliNameOffset);     // offset of chapter name in nametable
                tocEntry.AddRange(Utils.Mobi.EncodeVWI(0, true)); // Depth -- always 0.

                logicalTOCEntries.Add(tocEntry.ToArray());

                logicalTOCLabels.AddRange(vliNameLen);
                logicalTOCLabels.AddRange(chapterName.Encode());
            }
        }

        /// <summary>
        /// 
        /// The tag table entries are 4 bytes each:
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
            records[0] = MetaINDX();
            records[1] = DataINDX();
            records[2] = logicalTOCLabels.ToArray();
            return records;
        }

        private byte[] MetaINDX()
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
            int padding;
            byte[] Rec = logicalTOCEntries.Last();
            Rec = Rec.SubArray(0, Rec[0] + 1);
            padding = (Rec.Length + 2) % 4;

            // Make indx
            Records.INDX indx = new Records.INDX();
            indx.type = 2;                          // inflection
            indx.recordCount = 1;                   // num of indx data records
            indx.recordEntryCount = (uint)Chapters.Length;
            indx.idxtOffset = (uint)(indx.length + tagx.Count + (Rec.Length + 2 + padding));
            indx.cncxRecordCount = 1;
            indx.tagxOffset = indx.length;

            // Combine
            List<byte> record = new List<byte>();
            record.AddRange(indx.Dump());
            record.AddRange(tagx);
            record.AddRange(Rec);
            record.AddRange(Utils.BigEndian.GetBytes((ushort)idxtOffsets.Count));
            record.AddRange(new byte[padding]);
            record.AddRange("IDXT".Encode());
            record.AddRange(Utils.BigEndian.GetBytes((ushort)(indx.length + tagx.Count)));
            record.AddRange(new byte[2]);

            return record.ToArray();
        }

        private byte[] DataINDX()
        {
            List<byte> record = new List<byte>();

            Records.INDX indx = new Records.INDX();
            indx.type = 0;                              // normal
            indx.unused2 = new byte[] { 0, 0, 0, 1 };   // this should be one with type=0 because reasons
            indx.encoding = 0xFFFFFFFF;
            indx.idxtOffset = (uint)(indx.length + logicalTOCEntries.TotalLength());
            indx.recordCount = (uint)idxtOffsets.Count;

            record.AddRange(indx.Dump());
            foreach (byte[] rec in logicalTOCEntries)
            {
                record.AddRange(rec);
            }

            record.AddRange("IDXT".Encode());
            foreach (ushort offset in idxtOffsets)
            {
                record.AddRange(Utils.BigEndian.GetBytes(offset));
            }
            record.AddRange(new byte[(idxtOffsets.Count * 2) % 4]); // pad idxt to multiple of four

            return record.ToArray();
        }

        #endregion

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
