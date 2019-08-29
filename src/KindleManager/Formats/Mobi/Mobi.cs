using ExtensionMethods;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EXTHKey = Formats.Mobi.Headers.EXTHKey;

namespace Formats.Mobi
{
    public class Book : BookBase
    /* https://wiki.mobileread.com/wiki/MOBI
       All numerical values are big-endian.
       
    This file format is.... interesting. Its a war crime. Whoever designed
        this should be tried for crimes against humanity. They took something
        relatively simple and made it as obtuse as possible.
        MobileRead has some info about it but presents it in a very confusing
        way. Probably because trying to put any of this into plain english is
        almost as difficult as figuring out what any of this actually does.

        So here is my attempt:


    First the PDBHeader. Length is 0x4E bytes + 0x8 * recordCount. The only
        field you should modify is title, the first 32 bytes. This contains
        a truncated copy of the title, or the title padded with null bytes.

        records begin immediately after the PDBHeader. The length is 0x8 * the
        number of records.

        The first 4 bytes are a uint that describes the offset of the record
        from 0x10, or the end of the table at the beginning of this record.
        The 5th byte is the attributes of the Record. I've only ever seen this
        equal 0x0;
        Byte 6-8 are a 24-bit uint containing the uid of the record. UIDs seem
        to be all even numbers in order from 0, 2, 4, etc...

        All record data is adjacent, so the end of Record[n] is Record[n+1].
        Records can also have trailing data at their ends that must be trimmed
        off before decompressing. This is calculated using the MobiHeader's
        multibyte and trailers fields and requires several cups of coffee and 
        a bottle of aspirin to make sense.

        Trailing entries are appended directly to the end of the text record
        and end and the beginning of the next record. The size of each entry
        is indicated by an arbitrary number of bytes and the end of the record.
        Working backward from the end of the record, take bytes until one is
        read that has bit one set. Set this bit to zero and use all of these
        bytes to make a big-endian integer. This is the length in bytes of the
        entire trailing entry, including the bytes that make the size.


        I store records in a uint[]. The uid of the record is its index * 2,
        and it is safe to assume attributes are 0.

        Record compression is indicated by the PalmDOCHeader.compression flag.

    PalmDOCHeader contains a bit of information about the actual text content
        of the book. It is always 0x10 bytes and starts immediately after the 
        last record in PDBHeader.

    MobiHeader starts immediately after PalmDOC.
    
        This header contains tons of information about the book.
        The only field that should be changed are fullTitle, fullTitleLength,
        and fullTitleOffset. The complete title of the book is stored after
        all of the headers and is followed by a rather large amount of
        zero-padding.

        If the headerLength is > 0xE4 we will find data about the trailing
        bytes at the end of each record mentioned above. These flags are found
        in a ushort at this header's beginning + 0xf2 bytes.

        The extradataflags is an abomination that gives us two values.
        The lowest bit is a bool for multibyte. Ignoring this bit, every set
        bit in this ushort counts as one trailing byte appended to every
        record. This value can therefore not exceed 15. Because rather than
        storing the actual value of 15 in four bits that can be easily parsed
        by built-in methods in any language they managed to spread it around
        15 bits that require a special parsing mechanism.
        
    EXTHHeader starts at MobiHeader.offset + MobiHeader.headerLength.
        The first 0xC bytes is one string and two uints. The string is an ID
        that matched EXTH. The uints describe the total length of this header
        (including the pre-header) and the number of records in the header.
        Each record has a uint indicating its type, a uint indicating its
        length (including the id and length bytes), and the contents.
        There can be duplicate keys in this header. Mobileread has a
        full chart of the IDs.

    Immediately after the EXTHHeader should be the fullTitle, matching the
        offset in the MobiHeader. This may not have to start exactly after
        the EXTHHeader since there is a large amount of zero-padding between
        the end of the fullTitle and the start of the book html. There doesn't
        seem to be any standard as to how much padding there is.
     */

    {
        public Headers.PDBHeader PDBHeader = new Headers.PDBHeader();
        public Headers.PalmDOCHeader PalmDOCHeader = new Headers.PalmDOCHeader();
        public Headers.MobiHeader MobiHeader = new Headers.MobiHeader(Headers.MobiHeaderType.Mobi6);
        public Headers.EXTHHeader EXTHHeader = new Headers.EXTHHeader();

        public uint contentOffset;
        private int[] textRecordLengths;

        public Book(string filepath)
        {
            FilePath = filepath;

            using (BinaryReader reader = new BinaryReader(new FileStream(filepath, FileMode.Open)))
            {
                if (reader.BaseStream.Length < (PDBHeader.baseLength + PalmDOCHeader.length + MobiHeader.length))
                {
                    throw new FileFormatException("File is too short to contain all required header data");
                }

                reader.BaseStream.Seek(0, SeekOrigin.Begin);

                PDBHeader.Parse(reader);
                contentOffset = PDBHeader.records[1];

                this.PalmDOCHeader.offset = PDBHeader.records[0];
                this.PalmDOCHeader.Parse(reader);

                this.MobiHeader.offset = (uint)reader.BaseStream.Position;
                this.MobiHeader.Parse(reader);

                textRecordLengths = new int[MobiHeader.lastContentRecord - MobiHeader.firstContentRecord];
                for (uint i = 0; i < textRecordLengths.Length; i++)
                {
                    textRecordLengths[i] = (int)PDBHeader.RecordLength(MobiHeader.firstContentRecord + i);
                }

                if (this.MobiHeader.hasEXTH)
                {
                    EXTHHeader.offset = MobiHeader.offset + MobiHeader.length;
                    EXTHHeader.Parse(reader);

                    Author = EXTHHeader.Get(EXTHKey.Author).Decode();

                    byte[] isbn = EXTHHeader.Get<uint, byte[]>(104);
                    if (isbn != null && isbn.Length == 4)
                    {
                        ISBN = Utils.BigEndian.ToUInt32(isbn, 0x0);
                    }

                    Language = EXTHHeader.Get(EXTHKey.Language).Decode();
                    Contributor = EXTHHeader.Get(EXTHKey.Contributor).Decode();
                    Publisher = EXTHHeader.Get(EXTHKey.Publisher).Decode();
                    Subject = EXTHHeader.Get(EXTHKey.Subject).Decode().Split(',');
                    Description = EXTHHeader.Get(EXTHKey.Description).Decode();
                    PubDate = EXTHHeader.Get(EXTHKey.PublishDate).Decode();
                    Rights = EXTHHeader.Get(EXTHKey.Rights).Decode();

                }
                Title = MobiHeader.fullTitle;
            }
        }

        public Book(BookBase book)
        {
            BookBase.Merge(book, this);
        }

        /// <summary>
        ///
        /// Similar to FixLinks6 except...
        /// 
        /// For version 8+ the anchor tags look like this:
        ///     <a href="kindle:pos:fid:00A1:off:000123C">Chapter one</a>
        ///     Two numbers, fid and off, are base-32 integers. fid indicates the text
        ///         record in which to start counting, and off indicates how many bytes
        ///         to count from the start of this record.
        /// 
        /// </summary>
        private string FixLinks8()
        {
            throw new NotImplementedException();

            //{   /* For mobi version 8 or greater the filepos is stored in the
            //    href attribute as `kindle:pos:fid:0123:off:0123456789` where
            //    0123 indicates the text record containing the target and
            //    0123456789 indicates the offset inside this record.
            //    The total offset is calculated for filePositions.
            //    */
            //    anchors = doc.DocumentNode.SelectNodes("//a");
            //    if (anchors != null)
            //    {
            //        foreach (HtmlNode a in anchors)
            //        {
            //            int ol = a.OuterHtml.Length;
            //            string href = a.GetAttributeValue("href", "");
            //            if (href == "") continue;
            //            string[] parts = href.Split(':');
            //            if (parts.Length != 6) continue;

            //            if (!Utils.Base32Int.TryParse(parts[3], out int textRec)) continue;
            //            if (!Utils.Base32Int.TryParse(parts[3], out int offs)) continue;

            //            int filePos = 0;
            //            if (textRec == 1)
            //            {
            //                filePos = offs;
            //            }
            //            else
            //            {
            //                for (uint i = 0; i < textRec - 1; i++)
            //                {
            //                    filePos += textRecordLengths[i];
            //                }
            //                filePos += offs;
            //            }
            //            a.SetAttributeValue("href", filePos.ToString($"D10").PadRight(href.Length));
            //            filePositions.Add(filePos);
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Fixes image sources and anchor tags using "filepos" in html
        /// 
        /// Because everything in a mobi has to be much more complex than neccesary I'll explain this...
        ///       
        /// The filepos indicates a position in the un-decoded bytes that is somewhere near the opening '<' of the
        /// target node for the link.
        /// Because of this, the html has to be decoded (ie to utf-8) and parsed for anchors' filepos values.
        /// Then back to bytes to find where the target node is.
        /// Then back to the decoded string to manipulate it.
        /// 
        /// Images are easier. An image will have a recindex=00003 attribute instead of src. This points to the image
        /// record in PDBHeader starting with firstImageRecord as 1.
        /// 
        /// </summary>
        private string FixLinks6(HtmlDocument doc)
        {
            HtmlNodeCollection anchors;
            List<int> filePositions = new List<int>();
            int bytesAdded = 0;

            filePositions.Sort();

            // Find element closest to offset and apply id
            int ind = 0;
            HtmlNodeCollection children = doc.DocumentNode.ChildNodes;
            foreach (int offset in filePositions)
            {
                HtmlNode child = null;
                for (; ind < children.Count - 1; ind++)
                {
                    if (children[ind].BytePosition() > offset + bytesAdded)
                    {
                        child = children[ind == 0 ? ind : ind - 1];
                        break;
                    }
                }
                if (child == null)
                {
                    ind = 0;
                    continue;
                }
                else
                {
                    int ol = child.OuterLength;
                    child.SetAttributeValue("id", $"filepos{offset.ToString("D10")}");
                    bytesAdded += child.OuterLength - ol;
                }
            }

            // Switch filepos to href
            anchors = doc.DocumentNode.SelectNodes("//a");
            if (anchors != null)
            {
                foreach (HtmlNode a in anchors)
                {
                    string filePos = a.GetAttributeValue("filepos", null);
                    if (filePos == null) continue;

                    if (!int.TryParse(filePos, out int _)) continue;

                    a.SetAttributeValue("filepos", filePos); // for mobi 8+
                    a.SetAttributeValue("href", $"#filepos{filePos}");
                    a.SetAttributeValue("tocLabel", a.InnerText.Trim());
                }
            }

            HtmlNodeCollection imgs = doc.DocumentNode.SelectNodes("//img");
            if (imgs != null)
            {
                foreach (HtmlNode img in imgs)
                {
                    string recIndex = img.GetAttributeValue("recindex", string.Empty);
                    if (recIndex == string.Empty) continue;
                    img.SetAttributeValue("src", $"{recIndex}.jpg");
                }
            }

            return doc.DocumentNode.OuterHtml;
        }

        #region IBook overrides

        private string _Title;
        public override string Title
        {
            get => _Title;
            set
            {
                MobiHeader.fullTitle = value;
                MobiHeader.fullTitleLength = (uint)value.Length;
                PDBHeader.title = value.Length > 0x20 ? value.Substring(0x0, 0x20) : value + new byte[0x20 - value.Length].Decode();
                PDBHeader.title = PDBHeader.title.Replace(' ', '_');
                EXTHHeader.Set(EXTHKey.UpdatedTitle, value.Encode());
                _Title = value;
            }
        }

        private ulong _ISBN;
        public override ulong ISBN
        {
            get => _ISBN;
            set
            {
                EXTHHeader.Set(EXTHKey.ISBN, value.ToString().Encode());
                _ISBN = value;
            }
        }

        private string _Author;
        public override string Author
        {
            get => _Author;
            set
            {
                EXTHHeader[100] = value.Encode();
                _Author = value;
            }
        }

        private string _Publisher;
        public override string Publisher
        {
            get => _Publisher;
            set
            {
                EXTHHeader[101] = value.Encode();
                _Publisher = value;
            }
        }

        private string _PubDate;
        public override string PubDate
        {
            get => _PubDate;
            set
            {
                value = Utils.Metadata.GetDate(value);
                EXTHHeader.Set(EXTHKey.PublishDate, Utils.Mobi.FormatDate(value).Encode());
                _PubDate = value;
            }
        }

        /// <summary>
        /// Builds list of <str chaptername, str textcontent> from book
        /// </summary>
        /// <returns></returns>
        public override Tuple<string, HtmlDocument>[] TextContent()
        {
            Utils.Decompressors.IDecompressor Decompressor = null;
            List<byte[]> decompressedRecords = new List<byte[]>();

            using (BinaryReader reader = new BinaryReader(new FileStream(this.FilePath, FileMode.Open)))
            {
                switch (PalmDOCHeader.compression)
                {
                    case 1: // None
                        Decompressor = new Utils.Decompressors.None();
                        break;
                    case 2: // PalmDoc  
                        Decompressor = new Utils.Decompressors.PalmDoc(MobiHeader.flagTrailingEntries, MobiHeader.flagMultiByte);
                        break;
                    case 17480: // HUFF/CDIC
                        byte[][] huffRecords = new byte[MobiHeader.huffRecordCount][];

                        for (uint i = 0; i < MobiHeader.huffRecordCount; i++)
                        {
                            var current = MobiHeader.huffRecordNum + i;
                            reader.BaseStream.Seek(PDBHeader.records[current], SeekOrigin.Begin);
                            huffRecords[i] = reader.ReadBytes((int)PDBHeader.RecordLength(current));
                        }

                        Decompressor = new Utils.Decompressors.HuffCdic(huffRecords);
                        break;
                    default:
                        throw new InvalidDataException($"Unknown compression type: {PalmDOCHeader.compression}");
                }

                for (int i = 1; i <= PalmDOCHeader.textRecordCount; i++)
                {
                    reader.BaseStream.Seek(PDBHeader.records[i], SeekOrigin.Begin);
                    byte[] compressedText = reader.ReadBytes((int)(PDBHeader.records[i + 1] - PDBHeader.records[i]));
                    byte[] x = Decompressor.Decompress(compressedText);
                    decompressedRecords.Add(x);
                }
            }

            List<Tuple<string, HtmlDocument>> chapters = new List<Tuple<string, HtmlDocument>>();
            if (MobiHeader.minVersion <= 6)
            {
                byte[] combined = new byte[decompressedRecords.TotalLength()];
                int index = 0;
                foreach (byte[] rec in decompressedRecords)
                {
                    rec.CopyTo(combined, index);
                    index += rec.Length;
                }
                string text = Decode(combined);
                (string, int)[] toc = BuildMobi6Toc(text);

                HtmlDocument begin = new HtmlDocument();
                begin.LoadHtml(Decode(combined.SubArray(0, toc[0].Item2)));
                chapters.Add(new Tuple<string, HtmlDocument>("", begin));

                HtmlDocument doc = new HtmlDocument();
                for (int i = 0; i < toc.Length; i++)
                {
                    string name = toc[i].Item1;
                    int start = toc[i].Item2;
                    int end = (i == toc.Length - 1) ? combined.Length : toc[i + 1].Item2;

                    string ch = Decode(combined.SubArray(start, end - start));

                    if (i != 1)
                    {
                        ch = "<head><body>" + ch;
                    }

                    doc.LoadHtml(ch);
                    HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//img");
                    if (nodes != null)
                    {
                        foreach (HtmlNode img in nodes)
                        {
                            string recIndex = img.GetAttributeValue("recindex", string.Empty);
                            if (recIndex == string.Empty) continue;
                            img.SetAttributeValue("src", $"{recIndex}.jpg");
                        }
                    }

                    nodes = doc.DocumentNode.SelectNodes(@"//*[name()='mbp:pagebreak']");
                    if (nodes != null)
                    {
                        foreach (HtmlNode mbp in nodes)
                        {
                            mbp.Remove();
                        }
                    }

                    chapters.Add(new Tuple<string, HtmlDocument>(name, doc));
                }

                foreach ((string _, HtmlDocument html) in chapters)
                {
                    FixLinks6(html);
                }
            }
            else
            {
                // TODO
                throw new NotImplementedException();
            }

            return chapters.ToArray();
        }

        /// <summary>
        /// Gets chapter names 
        /// </summary>
        /// <returns>
        /// (string, int) of chapter title, offset in html bytearray
        /// </returns>
        private ValueTuple<string, int>[] BuildMobi6Toc(string text)
        {
            List<ValueTuple<string, int>> tocData = new List<ValueTuple<string, int>>();

            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(text);

            HashSet<int> filePositions = new HashSet<int>();
            HtmlNodeCollection anchors = html.DocumentNode.SelectNodes("//a");
            if (anchors != null)
            {
                foreach (HtmlNode a in anchors)
                {
                    string filePos = a.GetAttributeValue("filepos", string.Empty);
                    if (int.TryParse(filePos, out int i))
                    {
                        filePositions.Add(i);
                    }
                }
            }

            int ind = 0;
            HtmlNodeCollection children = html.DocumentNode.SelectNodes("//*");
            foreach (int pos in filePositions.OrderBy(x => x))
            {
                HtmlNode child = null;
                for (; ind < children.Count; ind++)
                {
                    int bp = children[ind].BytePosition();
                    if (bp > pos)
                    {
                        child = children[ind == 0 ? ind : ind - 1];
                        break;
                    }
                }
                if (child == null)
                {
                    ind = 0;
                    continue;
                }
                else
                {
                    tocData.Add(new ValueTuple<string, int>(child.InnerText, pos));
                }
            }
            return tocData.ToArray();
        }

        private string Decode(byte[] bytes)
        {
            switch (MobiHeader.textEncoding)
            {
                case 1252:
                    return bytes.Decode("Windows-1252");
                case 65001:
                    return bytes.Decode();
                default:
                    throw new ArgumentException($"Invalid text encoding: {MobiHeader.textEncoding}");
            };
        }

        public override byte[][] Images()
        {
            uint imageCount = MobiHeader.lastContentRecord - MobiHeader.firstImageRecord;

            byte[][] images = new byte[imageCount][];
            using (BinaryReader reader = new BinaryReader(new FileStream(this.FilePath, FileMode.Open)))
            {
                for (int i = 0; i < imageCount; i++)
                {
                    uint recOffset = PDBHeader.records[MobiHeader.firstImageRecord + i];
                    uint recLen = PDBHeader.records[MobiHeader.firstImageRecord + i + 1] - recOffset;
                    reader.BaseStream.Seek(recOffset, SeekOrigin.Begin);
                    images[i] = reader.ReadBytes((int)recLen);
                }
            }
            return images;
        }

        public override byte[] StyleSheet()
        {
            if (MobiHeader.minVersion <= 6)
            {
                return new byte[0];
            }
            else
            { //todo
                throw new NotImplementedException();
            }
        }

        public override void WriteMetadata()
        {
            EXTHHeader.Set(EXTHKey.Contributor, "KindleManger [https://github.com/sawyersteven/KindleManager]".Encode());

            using (BinaryWriter writer = new BinaryWriter(new FileStream(this.FilePath, FileMode.Open)))
            {
                List<byte> headerDump = new List<byte>();

                headerDump.AddRange(PDBHeader.Dump());
                headerDump.AddRange(PalmDOCHeader.Dump());

                byte[] exth = EXTHHeader.Dump();
                MobiHeader.fullTitleOffset = (uint)headerDump.Count + MobiHeader.length + (uint)exth.Length;
                headerDump.AddRange(MobiHeader.Dump());

                headerDump.AddRange(exth);

                uint totalHeaderLen = (uint)headerDump.Count + MobiHeader.fullTitleLength;

                uint fillerLen = contentOffset - totalHeaderLen;
                if (fillerLen < 0)
                {
                    throw new NotImplementedException("Header length exceeds contentOffset and I haven't implemented this yet");
                }

                writer.BaseStream.Seek(0, SeekOrigin.Begin);

                writer.Write(headerDump.ToArray());
                writer.Write(MobiHeader.fullTitle.Encode());
                writer.Write(new byte[fillerLen]);
            }
        }

        #endregion

        public void PrintHeaders()
        {
            PDBHeader.Print();
            PalmDOCHeader.Print();
            MobiHeader.Print();
            EXTHHeader.Print();
        }
    }
}