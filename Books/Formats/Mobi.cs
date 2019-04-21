using System;
using System.Collections.Generic;
using System.IO;
using ExtensionMethods;
using HtmlAgilityPack;

namespace Formats
{
    public class Mobi : IBook
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
        public MobiHeaders.PDBHeader PDBHeader;
        public MobiHeaders.PalmDOCHeader PalmDOCHeader;
        public MobiHeaders.MobiHeader MobiHeader;
        public MobiHeaders.EXTHHeader EXTHHeader;

        private delegate byte[] Decompressor(byte[] buffer, int compressedLen);
        private Decompressor decompress;

        public uint contentOffset;

        public Mobi(string filepath)
        {
            FilePath = filepath;

            Utils.BitConverter.LittleEndian = false;

            using (BinaryReader reader = new BinaryReader(new FileStream(filepath, FileMode.Open)))
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);

                // Basic header metadata
                this.PDBHeader = new MobiHeaders.PDBHeader();
                PDBHeader.Parse(reader);
                contentOffset = PDBHeader.records[1];

                // PalmDOCHeader
                this.PalmDOCHeader = new MobiHeaders.PalmDOCHeader();
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
                this.MobiHeader = new MobiHeaders.MobiHeader();
                this.MobiHeader.offset = (uint)reader.BaseStream.Position; // PalmDOCHeader + 0x10
                this.MobiHeader.Parse(reader);

                // EXTHHeader
                this.EXTHHeader = new MobiHeaders.EXTHHeader();
                if (this.MobiHeader.hasEXTH)
                {
                    EXTHHeader.offset = MobiHeader.offset + MobiHeader.headerLength;
                    EXTHHeader.Parse(reader);

                    Author = EXTHHeader.Get(100);

                    byte[] isbn = EXTHHeader.Get<uint, byte[]>(104);
                    if (isbn != null && isbn.Length == 4)
                    {
                        ISBN = Utils.BitConverter.ToUInt32(isbn, 0x0);
                    }

                    Language = EXTHHeader.Get(524);
                    Contributor = EXTHHeader.Get(108);
                    Publisher = EXTHHeader.Get(101);
                    Subject = new string[] { EXTHHeader.Get(105) };
                    Description = EXTHHeader.Get(103);
                    PubDate = EXTHHeader.Get(106);
                    Rights = EXTHHeader.Get(109);

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
        /// record in PDBHeader starting with firstImageRecord as 1. These are written to disk as 123456.jpg.
        /// 
        /// </summary>
        private string fixLinks(string html)
        {
            string targetNode = "<a id=\"filepos{0}\"/>";
            byte[] htmlBytes = html.Encode();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            Console.WriteLine(htmlBytes.SubArray(1745, 200).Decode());
            //

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
                int offs;
                if (!int.TryParse(offset, out offs)) continue;

                byte[] tn = string.Format(targetNode, offset).Encode();

                int insertPos = nearestElementPos(htmlBytes, offs + bytesAdded);

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

        private int nearestElementPos(byte[] html, int search)
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
        /// Parse backward-encoded Mobipocket variable-width int
        /// </summary>
        /// <param name="buffer"> At least four bytes read from end of text record</param>
        /// <returns></returns>
        private int varLengthInt(byte[] buffer)
        {
            int varint = 0;
            int shift = 0;
            for (int i = 3; i >= 0; i--)
            {
                byte b = buffer[i];
                varint |= (b & 0x7f) << shift;
                if ((b & 0x80) > 0)
                {
                    break;
                }
                shift += 7;
            }
            return varint;
        }

        /// <summary>
        /// Calculate length of extra record bytes at end of text record
        /// </summary>
        /// Crawls backward through buffer to find length of all extra records
        private int calcExtraBytes(byte[] record)
        {
            int pos = record.Length;
            for (int _ = 0; _ < MobiHeader.flagTrailingEntries; _++)
            {
                pos -= varLengthInt(record.SubArray(pos - 4, 0x4));
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
                EXTHHeader.Set(503, value);
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
                EXTHHeader.Set("ISBN", value.ToString());
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

        /// <summary>
        /// Returns mobi-html from book as string with changes made to 
        ///     work as a standard epub html doc.
        /// </summary>
        /// <returns></returns>
        public string TextContent()
            // TODO links are still messed up.
        {
            List<byte> bytes = new List<byte>();
            using (BinaryReader reader = new BinaryReader(new FileStream(this.FilePath, FileMode.Open)))
            {
                for (int i = 1; i <= PalmDOCHeader.textRecordCount; i++)
                {
                    reader.BaseStream.Seek(PDBHeader.records[i], SeekOrigin.Begin);
                    byte[] compressedText = reader.ReadBytes((int)(PDBHeader.records[i + 1] - PDBHeader.records[i]));
                    int compressedLen = calcExtraBytes(compressedText);
                    byte[] x = decompress(compressedText, compressedLen);
                    bytes.AddRange(x);
                }
            }

            string text = "";
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
            return fixLinks(text);
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
            EXTHHeader.Set(108, "Lignum [https://github.com/sawyersteven/Lignum]");


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

namespace Formats.MobiHeaders{

    public class PDBHeader
    {
        public readonly int offset = 0x0;
        public readonly int length = 0x4E;

        public string title;
        public short attributes;
        public short version;
        public uint createdDate;
        public uint modifiedDate;
        public uint backupDate;
        public uint modificationNum;
        public uint appInfoId;
        public uint sortInfoID;
        public string type;
        public string creator;
        public uint uniqueIDseed;
        public uint nextRecordListID;
        public ushort recordCount;
        public uint[] records;

        /// <summary>
        /// Contains basic metadata for mobi including locations of other headers.
        /// </summary>
        public PDBHeader() { }

        public void Parse(BinaryReader reader)
        {

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            byte[] buffer = reader.ReadBytes(this.length);

            Utils.BitConverter.LittleEndian = false;

            title = buffer.SubArray(0x0, 0x20).Decode();
            attributes = Utils.BitConverter.ToInt16(buffer, 0x20);
            version = Utils.BitConverter.ToInt16(buffer, 0x22);
            createdDate = Utils.BitConverter.ToUInt32(buffer, 0x24);
            modifiedDate = Utils.BitConverter.ToUInt32(buffer, 0x28);
            backupDate = Utils.BitConverter.ToUInt32(buffer, 0x2C);
            modificationNum = Utils.BitConverter.ToUInt32(buffer, 0x30);
            appInfoId = Utils.BitConverter.ToUInt32(buffer, 0x34);
            sortInfoID = Utils.BitConverter.ToUInt32(buffer, 0x38);
            type = buffer.SubArray(0x3C, 0x4).Decode();
            creator = buffer.SubArray(0x40, 0x4).Decode();
            uniqueIDseed = Utils.BitConverter.ToUInt32(buffer, 0x44);
            nextRecordListID = Utils.BitConverter.ToUInt32(buffer, 0x48);
            recordCount = Utils.BitConverter.ToUInt16(buffer, 0x4C);

            records = new uint[recordCount];

            reader.BaseStream.Seek(0x4E, SeekOrigin.Begin);
            byte[] rawBuffer = reader.ReadBytes(0x8 * recordCount);

            Utils.BitConverter.LittleEndian = false;

            for (var i = 0; i < recordCount; i++)
            {
                records[i] = Utils.BitConverter.ToUInt32(rawBuffer, i * 0x8);
            }
        }

        public byte[] Dump()
        {
            Utils.BitConverter.LittleEndian = false;

            List<byte> output = new List<byte>();
            output.AddRange(title.Encode());
            output.AddRange(Utils.BitConverter.GetBytes(attributes));
            output.AddRange(Utils.BitConverter.GetBytes(version));
            output.AddRange(Utils.BitConverter.GetBytes(createdDate));
            output.AddRange(Utils.BitConverter.GetBytes(modifiedDate));
            output.AddRange(Utils.BitConverter.GetBytes(backupDate));
            output.AddRange(Utils.BitConverter.GetBytes(modificationNum));
            output.AddRange(Utils.BitConverter.GetBytes(appInfoId));
            output.AddRange(Utils.BitConverter.GetBytes(sortInfoID));
            output.AddRange(type.Encode());
            output.AddRange(creator.Encode());
            output.AddRange(Utils.BitConverter.GetBytes(uniqueIDseed));
            output.AddRange(Utils.BitConverter.GetBytes(nextRecordListID));
            output.AddRange(Utils.BitConverter.GetBytes((ushort)records.Length));

            for (int i = 0; i < records.Length; i++)
            {
                output.AddRange(Utils.BitConverter.GetBytes(records[i])); // offset
                output.AddRange(Utils.BitConverter.GetBytes((i * 2) & 0x00FFFFFF)); // attr + uid
            }

            return output.ToArray();
        }


        public void Write(BinaryWriter writer)
        {
            writer.BaseStream.Seek(this.offset, SeekOrigin.Begin);
            writer.Write(Dump());
        }

        public uint recordLength(uint recordNum)
        {
            return records[recordNum + 1] - records[recordNum];
        }

        public void Print()
        {
            Console.WriteLine($@"
PDBHeader:
    title: {title}
    attributes: {attributes}
    version: {version}
    created: {createdDate}
    modified: {modifiedDate}
    backup: {backupDate}
    modnum: {modificationNum}
    appInfoId: {appInfoId}
    sortInfoID: {sortInfoID}
    type: {type}
    creator: {creator}
    uniqueIDseed: {uniqueIDseed}
    nextRecordListID: {nextRecordListID}
    recordCount: {recordCount}
    Records:");
            for (int i = 0; i < records.Length; i++)
            {
                Console.WriteLine($"{i * 2}: ({records[i]}, 0)");
            }
        }       
    }

    public class PalmDOCHeader
    {
        public long offset;
        public readonly int length = 0x10;

        public ushort compression;
        public uint textLength;
        public ushort textRecordCount;
        public ushort recordSize;
        public ushort encryptionType;
        public ushort mysteryData;

        /// <summary>
        /// Data contained in PalmDOCHeader portion of mobi.
        /// </summary>
        public PalmDOCHeader() { }

        public void Parse(BinaryReader reader)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            byte[] buffer = reader.ReadBytes(length);

            Utils.BitConverter.LittleEndian = false;

            compression = Utils.BitConverter.ToUInt16(buffer, 0x0);
            // Skip 0x2 unused bytes
            textLength = Utils.BitConverter.ToUInt32(buffer, 0x4);
            textRecordCount = Utils.BitConverter.ToUInt16(buffer, 0x8);
            recordSize = Utils.BitConverter.ToUInt16(buffer, 0xA);
            encryptionType = Utils.BitConverter.ToUInt16(buffer, 0xC);
            mysteryData = Utils.BitConverter.ToUInt16(buffer, 0xE);
        }

        public byte[] Dump()
        {
            Utils.BitConverter.LittleEndian = false;

            List<byte> output = new List<byte>();
            output.AddRange(Utils.BitConverter.GetBytes(compression));
            output.AddRange(new byte[0x2]);
            output.AddRange(Utils.BitConverter.GetBytes(textLength));
            output.AddRange(Utils.BitConverter.GetBytes(textRecordCount));
            output.AddRange(Utils.BitConverter.GetBytes(recordSize));
            output.AddRange(Utils.BitConverter.GetBytes(encryptionType));
            output.AddRange(Utils.BitConverter.GetBytes(mysteryData));
            return output.ToArray();
        }

        public void Write(BinaryWriter writer)
        {
            writer.BaseStream.Seek(this.offset, SeekOrigin.Begin);
            writer.Write(Dump());
        }

        public void Print()
        {
            Console.WriteLine($@"
PALMDOC:
    compression: {compression}
    textLength: {textLength}
    textRecordCount: {textRecordCount}
    recordSize: {recordSize}
    encryptionType: {encryptionType}
    mysteryData: {mysteryData}
            ");
        }
    }

    public class MobiHeader
    {
        public uint offset;
        public int length = 0xC8;
        // ^ Not the actual length, just the length we must read to fill
        // the fields below.

        public byte[] identifier;
        public uint headerLength;
        public uint mobiType;
        public uint textEncoding;
        public uint uid;
        public uint generatorVersion;
        public byte[] indexes;
        public uint firstNonBookIndex;
        public uint fullTitleOffset;
        public uint fullTitleLength;
        public uint language;
        public uint inputLanguage;
        public uint outputLanguage;
        public uint formatVersion;
        public uint firstImageRecord;
        public uint huffRecordOffset;
        public uint huffRecordCount;
        public uint datpRecordOffset;
        public uint datpRecordCount;
        public uint exthFlags;
        public byte[] unknown1;
        public uint drmOffset;
        public uint drmCount;
        public uint drmSize;
        public uint drmFlags;
        public byte[] unknown2;
        public ushort firstContentRecord;
        public ushort lastContentRecord;
        public byte[] unknown3;
        public uint fcisRecord;
        public byte[] unknown4;
        public uint flisRecord;
        public byte[] unknown5;
        // This only exists if the headerlength is 0xe4 or 0xe8
        public uint extraDataFlags;
        public uint indxRecordOffset;

        public bool flagMultiByte;
        public uint flagTrailingEntries;
        public bool hasDRM;
        public bool hasEXTH;
        public string fullTitle;

        /// <summary>
        /// Data contained in MobiHeader portion of mobi.
        /// All offsets are relative to 0x0
        /// </summary>
        public MobiHeader() { }
        
        public void Parse(BinaryReader reader)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            byte[] buffer = reader.ReadBytes(length);

            Utils.BitConverter.LittleEndian = false;

            identifier = buffer.SubArray(0x0, 0x4); // MOBI
            headerLength = Utils.BitConverter.ToUInt32(buffer, 0x4);
            mobiType = Utils.BitConverter.ToUInt32(buffer, 0x8);
            textEncoding = Utils.BitConverter.ToUInt32(buffer, 0xC);
            uid = Utils.BitConverter.ToUInt32(buffer, 0x10);
            generatorVersion = Utils.BitConverter.ToUInt32(buffer, 0x14);
            indexes = buffer.SubArray(0x18, 0x28);
            firstNonBookIndex = Utils.BitConverter.ToUInt32(buffer, 0x40);
            fullTitleOffset = Utils.BitConverter.ToUInt32(buffer, 0x44) + offset - 0x10;
            // ^ Offset is from PDBHeader table, or 0x10.
            fullTitleLength = Utils.BitConverter.ToUInt32(buffer, 0x48);
            language = Utils.BitConverter.ToUInt32(buffer, 0x4C);
            inputLanguage = Utils.BitConverter.ToUInt32(buffer, 0x50);
            outputLanguage = Utils.BitConverter.ToUInt32(buffer, 0x54);
            formatVersion = Utils.BitConverter.ToUInt32(buffer, 0x58);
            firstImageRecord = Utils.BitConverter.ToUInt32(buffer, 0x5C);
            huffRecordOffset = Utils.BitConverter.ToUInt32(buffer, 0x60);
            huffRecordCount = Utils.BitConverter.ToUInt32(buffer, 0x64);
            datpRecordOffset = Utils.BitConverter.ToUInt32(buffer, 0x68);
            datpRecordCount = Utils.BitConverter.ToUInt32(buffer, 0x6C);
            exthFlags = Utils.BitConverter.ToUInt32(buffer, 0x70);
            unknown1 = buffer.SubArray(0x74, 0x24);
            drmOffset = Utils.BitConverter.ToUInt32(buffer, 0x98);
            drmCount = Utils.BitConverter.ToUInt32(buffer, 0x9C);
            drmSize = Utils.BitConverter.ToUInt32(buffer, 0xA0);
            drmFlags = Utils.BitConverter.ToUInt32(buffer, 0xA4);
            unknown2 = buffer.SubArray(0xA8, 0x8);
            firstContentRecord = Utils.BitConverter.ToUInt16(buffer, 0xB0);
            lastContentRecord = Utils.BitConverter.ToUInt16(buffer, 0xB2);
            unknown3 = buffer.SubArray(0xB4, 0x4);
            fcisRecord = Utils.BitConverter.ToUInt32(buffer, 0xB8);
            unknown4 = buffer.SubArray(0xBC, 0x4);
            flisRecord = Utils.BitConverter.ToUInt32(buffer, 0xC0);
            unknown5 = buffer.SubArray(0xC4, 0x4);

            hasDRM = drmOffset != 0xFFFFFFFF;
            hasEXTH = (exthFlags & 0x40) != 0;

            reader.BaseStream.Seek(fullTitleOffset, SeekOrigin.Begin);
            fullTitle = reader.ReadBytes((int)fullTitleLength).Decode();

            if (headerLength >= 0xe4)
            {
                reader.BaseStream.Seek(offset + 0xe2, SeekOrigin.Begin);

                extraDataFlags = Utils.BitConverter.ToUInt16(reader.ReadBytes(0x2), 0x0);
                indxRecordOffset = Utils.BitConverter.ToUInt32(reader.ReadBytes(0x4), 0x0);
                
                flagMultiByte = (extraDataFlags & 0x01) == 1;
                extraDataFlags >>= 0x01;
                while (extraDataFlags > 0)
                {
                    flagTrailingEntries += extraDataFlags & 0x01;
                    extraDataFlags >>= 0x01;
                }
            }
        }

        public byte[] Dump()
        {
            Utils.BitConverter.LittleEndian = false;
            List<byte> output = new List<byte>();

            output.AddRange(identifier);
            output.AddRange(Utils.BitConverter.GetBytes(headerLength));
            output.AddRange(Utils.BitConverter.GetBytes(mobiType));
            output.AddRange(Utils.BitConverter.GetBytes(textEncoding));
            output.AddRange(Utils.BitConverter.GetBytes(uid));
            output.AddRange(Utils.BitConverter.GetBytes(generatorVersion));
            output.AddRange(indexes);
            output.AddRange(Utils.BitConverter.GetBytes(firstNonBookIndex));
            output.AddRange(Utils.BitConverter.GetBytes(fullTitleOffset - offset + 0x10));
            output.AddRange(Utils.BitConverter.GetBytes(fullTitleLength));
            output.AddRange(Utils.BitConverter.GetBytes(language));
            output.AddRange(Utils.BitConverter.GetBytes(inputLanguage));
            output.AddRange(Utils.BitConverter.GetBytes(outputLanguage));
            output.AddRange(Utils.BitConverter.GetBytes(formatVersion));
            output.AddRange(Utils.BitConverter.GetBytes(firstImageRecord));
            output.AddRange(Utils.BitConverter.GetBytes(huffRecordOffset));
            output.AddRange(Utils.BitConverter.GetBytes(huffRecordCount));
            output.AddRange(Utils.BitConverter.GetBytes(datpRecordOffset));
            output.AddRange(Utils.BitConverter.GetBytes(datpRecordCount));
            output.AddRange(Utils.BitConverter.GetBytes(exthFlags));
            output.AddRange(unknown1);
            output.AddRange(Utils.BitConverter.GetBytes(drmOffset));
            output.AddRange(Utils.BitConverter.GetBytes(drmCount));
            output.AddRange(Utils.BitConverter.GetBytes(drmSize));
            output.AddRange(Utils.BitConverter.GetBytes(drmFlags));
            output.AddRange(unknown2);
            output.AddRange(Utils.BitConverter.GetBytes(firstContentRecord));
            output.AddRange(Utils.BitConverter.GetBytes(lastContentRecord));
            output.AddRange(unknown3);
            output.AddRange(Utils.BitConverter.GetBytes(fcisRecord));
            output.AddRange(unknown4);
            output.AddRange(Utils.BitConverter.GetBytes(flisRecord));
            output.AddRange(unknown5);

            int missingBytes = (int)(headerLength - output.Count);
            if (missingBytes > 0)
            {
                byte[] filler = new byte[missingBytes];
                for (int i = 0; i < filler.Length; i++)
                {
                    filler[i] = 0xFF;
                }
                output.AddRange(filler);
            }

            return output.ToArray();
        }

        public void Write(BinaryWriter writer)
        {
            writer.BaseStream.Seek(this.offset, SeekOrigin.Begin);
            writer.Write(Dump());
        }

        public void WriteTitle(BinaryWriter writer)
        {
            writer.BaseStream.Seek(fullTitleOffset, SeekOrigin.Begin);
            writer.Write(fullTitle.Encode());
        }

        public void Print()
        {
            Console.WriteLine($@"
MOBI:
    identifier: {identifier.Decode()}
    headerLength: {headerLength}
    mobiType: {mobiType}
    textEncoding: {textEncoding}
    uid: {uid}
    generatorVersion: {generatorVersion}
    firstNonBookIndex: {firstNonBookIndex}
    fullTitleOffset: {fullTitleOffset}
    fullTitleLength: {fullTitleLength}
    language: {language}
    inputLanguage: {inputLanguage}
    outputLanguage: {outputLanguage}
    formatVersion: {formatVersion}
    firstImageRecord: {firstImageRecord}
    huffRecordOffset: {huffRecordOffset}
    huffRecordCount: {huffRecordCount}
    datpRecordOffset: {datpRecordOffset}
    datpRecordCount: {datpRecordCount}
    exthFlags: {exthFlags}
    drmOffset: {drmOffset}
    drmCount: {drmCount}
    drmSize: {drmSize}
    drmFlags: {drmFlags}
    firstContentRecord: {firstContentRecord}
    lastImageRecord: {lastContentRecord}
    fcisRecord: {fcisRecord}
    flisRecord: {flisRecord}
            ");
        }
    }

    public class EXTHHeader : Dictionary<uint, byte[]>
    {
        public long offset;
        public int length;

        public byte[] identifier;
        public uint recordCount;

        public EXTHHeader() { }

        public static Dictionary<string, uint> RecordNames = new Dictionary<string, uint>()
        {
            {"Author", 100},
            {"Publisher", 101},
            {"Imprint", 102},
            {"Description", 103},
            {"ISBN", 104},
            {"Subject", 105},
            {"PublishDate", 106},
            {"Review", 107},
            {"Contributor", 108},
            {"Rights", 109},
            {"SubjectCode", 110},
            {"Type", 111},
            {"Source", 112},
            {"ASIN", 113},
            {"VersionNumber", 114},
            {"IsSample", 115},
            {"StartReading", 116},
            {"RetailPrice", 118},
            {"RetailPriceCurrency", 119},
            {"DictShortName", 200},
            {"Creator", 204},
            {"CDEType", 501},
            {"UpdatedTitle", 503},
            {"ASIN2", 504},
            {"Language", 524}
        };

        public void Set(string rec, string val)
        {
            if (RecordNames.TryGetValue(rec, out uint key))
            {
                this[key] = val.Encode();
            }
        }

        public void Set(uint rec, string val)
        {
            this[rec] = val.Encode();
        }

        public string Get(string rec)
        {
            byte[] val = new byte[0];
            if (RecordNames.TryGetValue(rec, out uint key))
            {
                this.TryGetValue(key, out val);
            }
            return val.Decode();
        }

        public string Get(uint rec)
        {
            if (this.TryGetValue(rec, out byte[] val))
            {
                return val.Decode();
            }
            else
            {
                return string.Empty;
            }
        }

        public void Parse(BinaryReader reader)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            byte[] buffer = reader.ReadBytes(0xC);

            Utils.BitConverter.LittleEndian = false;

            identifier = buffer.SubArray(0x0, 0x4);
            length = (int)Utils.BitConverter.ToUInt32(buffer, 0x4);
            recordCount = Utils.BitConverter.ToUInt32(buffer, 0x8);

            for (int i = 0; i < recordCount; i++)
            {
                buffer = reader.ReadBytes(0x8);
                var recType = Utils.BitConverter.ToUInt32(buffer, 0x0);
                var recLen = Utils.BitConverter.ToUInt32(buffer, 0x4) - 0x8;
                var recData = reader.ReadBytes((int)recLen);
                if (!this.ContainsKey(recType))
                {
                    this.Add(recType, recData);
                }
            }
        }

        public byte[] Dump()
        {
            Utils.BitConverter.LittleEndian = false;

            List<byte> output = new List<Byte>();
            output.AddRange(identifier);
            output.AddRange(Utils.BitConverter.GetBytes((uint)length));
            output.AddRange(Utils.BitConverter.GetBytes((uint)Keys.Count));
            foreach (var kv in this)
            {
                output.AddRange(Utils.BitConverter.GetBytes(kv.Key)); // recType
                output.AddRange(Utils.BitConverter.GetBytes((uint)kv.Value.Length + 0x8));
                output.AddRange(kv.Value); // recData
            }
            return output.ToArray();
        }

        public void Write(BinaryWriter writer)
        {
            writer.BaseStream.Seek(offset, SeekOrigin.Begin);
            writer.Write(Dump());
        }

        public void Print()
        {
            Console.WriteLine($@"
EXTH HEADER:
    identifier: {identifier}
    recordCount: {recordCount}
            ");
            foreach (uint k in this.Keys){
                Console.WriteLine($"\t{k}: {Get(k)}");

            }
        }

    }

}
