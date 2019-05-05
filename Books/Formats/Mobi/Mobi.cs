using System;
using System.Collections.Generic;
using System.IO;
using ExtensionMethods;
using HtmlAgilityPack;
using System.Linq;

using EXTHRecordID = Formats.Mobi.Headers.EXTHRecordID;

namespace Formats.Mobi
{
    public class Book : IBook
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

        This header not 0xC8 bytes long, but we have to read those bytes frist
        to find out how long the whole thing is. The field headerLength is
        a lie and also not the actual length of the whole header. But if the
        headerLength is 0xE4 or 0xE8 we will find data about the trailing
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
        public Headers.PDBHeader PDBHeader;
        public Headers.PalmDOCHeader PalmDOCHeader;
        public Headers.MobiHeader MobiHeader;
        public Headers.EXTHHeader EXTHHeader;

        private delegate byte[] Decompressor(byte[] buffer, int compressedLen);
        private readonly Decompressor decompress;

        public uint contentOffset;

        public Book(string filepath)
        {
            FilePath = filepath;

            using (BinaryReader reader = new BinaryReader(new FileStream(filepath, FileMode.Open)))
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);

                // Basic header metadata
                this.PDBHeader = new Headers.PDBHeader();
                PDBHeader.Parse(reader);
                contentOffset = PDBHeader.records[1];

                // PalmDOCHeader
                this.PalmDOCHeader = new Headers.PalmDOCHeader();
                this.PalmDOCHeader.offset = PDBHeader.records[0];
                this.PalmDOCHeader.Parse(reader);
                switch (PalmDOCHeader.compression)
                {
                    case 1: // None
                        decompress = (byte[] buffer, int compressedLen) => buffer.SubArray(0, compressedLen);
                        break;
                    case 2: // PalmDoc
                        decompress = Utils.PalmDoc.decompress;
                        break;
                    case 17480: // HUFF/CDIC
                        throw new NotImplementedException("HUFF/CDIC compression not implemented");
                }

                // MobiHeader
                this.MobiHeader = new Headers.MobiHeader();
                this.MobiHeader.offset = (uint)reader.BaseStream.Position;
                this.MobiHeader.Parse(reader);

                // EXTHHeader
                this.EXTHHeader = new Headers.EXTHHeader();
                if (this.MobiHeader.hasEXTH)
                {
                    EXTHHeader.offset = MobiHeader.offset + MobiHeader.length;
                    EXTHHeader.Parse(reader);

                    Author = EXTHHeader.Get(Headers.EXTHRecordID.Author).Decode();

                    byte[] isbn = EXTHHeader.Get<uint, byte[]>(104);
                    if (isbn != null && isbn.Length == 4)
                    {
                        ISBN = Utils.BigEndian.ToUInt32(isbn, 0x0);
                    }

                    Language = EXTHHeader.Get(EXTHRecordID.Language).Decode();
                    Contributor = EXTHHeader.Get(EXTHRecordID.Contributor).Decode();
                    Publisher = EXTHHeader.Get(EXTHRecordID.Publisher).Decode();
                    Subject = new string[] { EXTHHeader.Get(EXTHRecordID.Subject).Decode() };
                    Description = EXTHHeader.Get(EXTHRecordID.Description).Decode();
                    PubDate = EXTHHeader.Get(EXTHRecordID.PublishDate).Decode();
                    Rights = EXTHHeader.Get(EXTHRecordID.Rights).Decode();

                }
                Title = MobiHeader.fullTitle;
            }
        }

        /// <summary>
        /// Fixes image sources and anchor tags using "filepos" in html
        /// 
        /// Because everything in a mobi has to be much more complex than neccesary I'll explain this...
        /// Anchor tags have a filepos attribute like this <a filepos=000012345>Chapter one</a>
        /// The filepos indicates a position in the un-decoded bytes that is somewhere near the actual target for the link
        /// Because of this, the html has to be decoded (ie to utf-8) and parsed for anchors' filepos values.
        /// Then we go back to the un-decoded bytes and insert a new node with the target id to make things easier.
        /// 
        /// Images are easier. An image will have a recindex=123456 attribute instead of src. This points to the image
        /// record in PDBHeader starting with firstImageRecord as 1.
        /// 
        /// </summary>
        private string FixLinks(string html)
        {
            string targetNode = "<a id=\"filepos{0}\"/>";
            byte[] htmlBytes = html.Encode();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            List<string> filePositions = new List<string>();

            foreach (HtmlNode a in doc.DocumentNode.SelectNodes("//a"))
            {
                string filePos = a.GetAttributeValue("filepos", "");
                if (filePos == "") continue;
                filePositions.Add(filePos);
            }

            filePositions.Sort();

            int bytesAdded = 0;
            foreach (string offset in filePositions)
            {
                if (!int.TryParse(offset, out int offs)) continue;

                byte[] tn = string.Format(targetNode, offset).Encode();

                int insertPos = NearestElementPos(htmlBytes, offs + bytesAdded);
                HtmlNode target = doc.DocumentNode.ChildNodes.FirstOrDefault(x => x.BytePosition() == insertPos);

                htmlBytes = htmlBytes.InsertRange(insertPos, tn);

                bytesAdded += tn.Length;
            }

            html = htmlBytes.Decode();
            doc.LoadHtml(html);

            HtmlNodeCollection anchors = doc.DocumentNode.SelectNodes("//a");
            if (anchors != null)
            {
                foreach (HtmlNode a in anchors)
                {
                    string filePos = a.GetAttributeValue("filepos", string.Empty);
                    if (filePos == string.Empty) continue;
                    a.SetAttributeValue("href", $"#filepos{filePos}");
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

        private int NearestElementPos(byte[] html, int search)
        {
            if (search > html.Length) return -1;
            if (html[search] == '<') return search - 1;

            while (html[search] != '<')
            {
                search--;
            }

            return search - 1;
        }

        /// <summary>
        /// Calculate length of extra record bytes at end of text record
        /// </summary>
        /// Crawls backward through buffer to find length of all extra records
        private int CalcExtraBytes(byte[] record)
        {
            int pos = record.Length;
            for (int _ = 0; _ < MobiHeader.flagTrailingEntries; _++)
            {
                pos -= Utils.Mobi.VarLengthInt(record.SubArray(pos - 4, 0x4));
            }
            if (MobiHeader.flagMultiByte)
            {
                pos -= (record[pos] & 0x3) + 1;
            }
            return pos;
        }

        #region IBook impl
        public string FilePath { get; set; }
        public string Format { get => "MOBI"; }

        private string _Title;
        public string Title
        {
            get => _Title;
            set
            {
                MobiHeader.fullTitle = value;
                MobiHeader.fullTitleLength = (uint)value.Length;
                PDBHeader.title = value.Length > 0x20 ? value.Substring(0x0, 0x20) : value + new byte[0x20 - value.Length].Decode();
                PDBHeader.title = PDBHeader.title.Replace(' ', '_');
                EXTHHeader.Set(EXTHRecordID.UpdatedTitle, value.Encode());
                _Title = value;
            }
        }

        private string _Language { get; set; }
        public string Language
        {
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
                EXTHHeader.Set(Headers.EXTHRecordID.ISBN, value.ToString().Encode());
                _ISBN = value;
            }
        }

        private string _Author;
        public string Author
        {
            get => _Author;
            set
            {
                EXTHHeader[100] = value.Encode();
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
        public string Publisher
        {
            get => _Publisher;
            set
            {
                EXTHHeader[101] = value.Encode();
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
        public string PubDate
        {
            get => _PubDate;
            set
            {
                value = Utils.Metadata.GetDate(value);
                EXTHHeader[106] = value.Encode();
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
        public string Series
        {
            get => _Series;
            set
            {
                _Series = value;
            }
        }

        private float _SeriesNum;
        public float SeriesNum
        {
            get => _SeriesNum;
            set
            {
                _SeriesNum = value;
            }
        }

        public string DateAdded { get; set; }

        public string RawText()
        {
            List<byte> bytes = new List<byte>();
            using (BinaryReader reader = new BinaryReader(new FileStream(this.FilePath, FileMode.Open)))
            {
                for (int i = 1; i <= PalmDOCHeader.textRecordCount; i++)
                {
                    reader.BaseStream.Seek(PDBHeader.records[i], SeekOrigin.Begin);
                    byte[] compressedText = reader.ReadBytes((int)(PDBHeader.records[i + 1] - PDBHeader.records[i]));
                    int compressedLen = CalcExtraBytes(compressedText);
                    byte[] x = decompress(compressedText, compressedLen);
                    bytes.AddRange(x);
                }
            }

            string text;
            switch (MobiHeader.textEncoding)
            {
                case 1252:
                    text = bytes.ToArray().Decode("CP1252");
                    break;
                case 65001:
                    text = bytes.ToArray().Decode();
                    break;
                default:
                    throw new ArgumentException($"Invalid text encoding: {MobiHeader.textEncoding}");
            };
            return text;

        }

        /// <summary>
        /// Returns mobi-html from book as string with changes made to 
        ///     work as a standard epub html doc.
        /// </summary>
        /// <returns></returns>
        public string TextContent()
        {
            List<byte> bytes = new List<byte>();
            using (BinaryReader reader = new BinaryReader(new FileStream(this.FilePath, FileMode.Open)))
            {
                for (int i = 1; i <= PalmDOCHeader.textRecordCount; i++)
                {
                    reader.BaseStream.Seek(PDBHeader.records[i], SeekOrigin.Begin);
                    byte[] compressedText = reader.ReadBytes((int)(PDBHeader.records[i + 1] - PDBHeader.records[i]));
                    int compressedLen = CalcExtraBytes(compressedText);
                    byte[] x = decompress(compressedText, compressedLen);
                    bytes.AddRange(x);
                }
            }

            string text;
            switch (MobiHeader.textEncoding)
            {
                case 1252:
                    text = bytes.ToArray().Decode("CP1252");
                    break;
                case 65001:
                    text = bytes.ToArray().Decode();
                    break;
                default:
                    throw new ArgumentException($"Invalid text encoding: {MobiHeader.textEncoding}");
            };
            return FixLinks(text);
        }

        public byte[][] Images()
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

        public void WriteMetadata()
        {
            EXTHHeader.Set(Headers.EXTHRecordID.Contributor, "Lignum [https://github.com/sawyersteven/Lignum]".Encode());


            using (BinaryWriter writer = new BinaryWriter(new FileStream(this.FilePath, FileMode.Open)))
            {
                List<byte> headerDump = new List<byte>();

                headerDump.AddRange(PDBHeader.Dump());
                headerDump.AddRange(PalmDOCHeader.Dump());
                headerDump.AddRange(MobiHeader.Dump());
                headerDump.AddRange(EXTHHeader.Dump());


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