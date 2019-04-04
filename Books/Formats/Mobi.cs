using System;
using System.Collections.Generic;
using System.IO;
using ExtensionMethods;
using System.Linq;

namespace Formats
{
    public class Mobi : IBook
    /* This file format is.... interesting. MobileRead has some info about it
        but presents it in a very confusing way. https://wiki.mobileread.com/wiki/MOBI
        All numerical values are big-endian.

    First the basic header. Always 0x4E bytes. The only part we want to change
        is the title section. The first 0x20 bytes holds the title and is
        truncated if longer than 0x20 characters. This ends with a ushort
        that tells us how many Records there are.

    Records begins immediately after the basic header. The length is 0x8 * the
        number of records.

        The first 4 bytes are a uint that describes the offset of the record
        from 0x0.
        The 5th byte is the attributes of the Record. I've only ever seen this
        equal 0x0;
        Byte 6-8 are a 24-bit uint containing the uid of the record.
        Records 8-byte records start with a uint for their uid. I don't know
        what their purpose is, but uid0 seems to point to the MobiHeader start.

    PalmDOCHeader contains a bit of information about the actual text content
        of the book. Nothing here is useful to us. It is always 0x10 bytes.

    MobiHeader starts immediately after PalmDOC. This header contains tons
        of information about the book. The only field that should be changed
        are fullTitle, fullTitleLength, and fullTitleOffset. The complete title
        of the book is stored after all of the headers and is followed by a
        rather large amount of zero-padding.

    EXTHHeader starts immediately after the MobiHeader. The first 0xC bytes
        is one string and two uints. The string is an ID that matched EXTH.
        The uints describe the total length of this header (including the
        pre-header) and the number of records in the header.
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
        public MobiHeaders.BaseHeader BaseHeader;
        public MobiHeaders.Records Records;
        public MobiHeaders.PalmDOCHeader PalmDOCHeader;
        public MobiHeaders.MobiHeader MobiHeader;
        public MobiHeaders.EXTHHeader EXTHHeader;

        public uint contentOffset;

        public Mobi(string filepath)
        {
            FilePath = filepath;

            Utils.BitConverter.LittleEndian = false;

            using (BinaryReader reader = new BinaryReader(new FileStream(filepath, FileMode.Open)))
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);

                // Basic header metadata
                this.BaseHeader = new MobiHeaders.BaseHeader();
                BaseHeader.Parse(reader);

                // Records
                this.Records = new MobiHeaders.Records();
                this.Records.Parse(reader, BaseHeader.numberOfRecords);
                contentOffset = Records[2].Item1;

                // PalmDOCHeader
                this.PalmDOCHeader = new MobiHeaders.PalmDOCHeader();
                this.PalmDOCHeader.offset = Records[0].Item1;
                this.PalmDOCHeader.Parse(reader);

                // MobiHeader
                this.MobiHeader = new MobiHeaders.MobiHeader();
                this.MobiHeader.offset = reader.BaseStream.Position;
                this.MobiHeader.Parse(reader, Records[0].Item1);
                Title = MobiHeader.fullTitle;

                // EXTHHeader
                this.EXTHHeader = new MobiHeaders.EXTHHeader();
                if (this.MobiHeader.hasEXTH)
                {
                    EXTHHeader.offset = this.MobiHeader.offset + this.MobiHeader.length;
                    EXTHHeader.Parse(reader);

                    if (EXTHHeader.ContainsKey(101))
                    {
                        Author = EXTHHeader.Get<uint, byte[]>(100).Decode();
                    }

                    if (EXTHHeader.ContainsKey(101))
                    {
                        Publisher = EXTHHeader[101].Decode();
                    }

                    if (EXTHHeader.ContainsKey(106))
                    {
                        PubDate = EXTHHeader.Get<uint, byte[]>(106).Decode();
                    }

                    if (EXTHHeader.ContainsKey(104))
                    {
                        ISBN = Utils.BitConverter.ToUInt32(EXTHHeader.Get<uint, byte[]>(104), 0x0);
                    }
                }
            }
        }

        #region IBook impl
        public int Id { get; set; }
        public string FilePath { get; set; }
        public string Type { get => "MOBI"; }

        private string _Title;
        public string Title {
            get => _Title;
            set
            {
                MobiHeader.fullTitle = value;
                MobiHeader.fullTitleLength = (uint)value.Length;
                BaseHeader.title = value.Length > 0x20 ? value.Substring(0x0, 0x20) : value + new byte[0x20 - value.Length].Decode();
                BaseHeader.title = BaseHeader.title.Replace(' ', '_');
                _Title = value;
            }
        }

        private string _Author;
        public string Author {
            get => _Author;
            set
            {
                EXTHHeader[100] = value.Encode();
                _Author = value;
            }
        }

        private string _Publisher;
        public string Publisher {
            get => _Publisher;
            set
            {
                EXTHHeader[101] = value.Encode();
                _Publisher = value;
            }
        }

        private string _PubDate;
        public string PubDate {
            get => _PubDate;
            set
            {
                value = Utils.Metadata.GetDate(value);
                EXTHHeader[106] = value.Encode();
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

            using (BinaryWriter writer = new BinaryWriter(new FileStream(this.FilePath, FileMode.Open)))
            {
                int totalHeaderLen = BaseHeader.length + Records.length + PalmDOCHeader.length + MobiHeader.length + EXTHHeader.length + (int)MobiHeader.fullTitleLength;

                uint fillerLen = contentOffset - (uint)totalHeaderLen;
                if (fillerLen < 0)
                {
                    throw new NotImplementedException("Header length exceeds contentOffset and I haven't implemented this yet");
                }

                writer.BaseStream.Seek(0, SeekOrigin.Begin);

                BaseHeader.Write(writer);
                Records.Write(writer);
                PalmDOCHeader.Write(writer);
                MobiHeader.Write(writer, Records[0].Item1);
                EXTHHeader.Write(writer);
                writer.BaseStream.Seek(MobiHeader.fullTitleOffset + MobiHeader.fullTitleLength, SeekOrigin.Begin);
                writer.Write(new byte[fillerLen]);
            }
        }
        #endregion
    }
}

namespace Formats.MobiHeaders{

    public class BaseHeader
    {
        public readonly int offset = 0x0;
        public readonly int length = 0x4E;

        public string title;
        public short attributes;
        public short version;
        public uint created;
        public uint modified;
        public uint backup;
        public uint modnum;
        public uint appInfoId;
        public uint sortInfoID;
        public string type;
        public string creator;
        public uint uniqueIDseed;
        public uint nextRecordListID;
        public ushort numberOfRecords;

        /// <summary>
        /// Contains basic metadata for mobi including locations of other headers.
        /// </summary>
        public BaseHeader() { }

        public void Parse(BinaryReader reader)
        {

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            byte[] buffer = reader.ReadBytes(this.length);

            Utils.BitConverter.LittleEndian = false;

            this.title = buffer.SubArray(0x0, 0x20).Decode();
            this.attributes = Utils.BitConverter.ToInt16(buffer, 0x20);
            this.version = Utils.BitConverter.ToInt16(buffer, 0x22);
            this.created = Utils.BitConverter.ToUInt32(buffer, 0x24);
            this.modified = Utils.BitConverter.ToUInt32(buffer, 0x28);
            this.backup = Utils.BitConverter.ToUInt32(buffer, 0x2C);
            this.modnum = Utils.BitConverter.ToUInt32(buffer, 0x30);
            this.appInfoId = Utils.BitConverter.ToUInt32(buffer, 0x34);
            this.sortInfoID = Utils.BitConverter.ToUInt32(buffer, 0x38);
            this.type = buffer.SubArray(0x3C, 0x4).Decode();
            this.creator = buffer.SubArray(0x40, 0x4).Decode();
            this.uniqueIDseed = Utils.BitConverter.ToUInt32(buffer, 0x44);
            this.nextRecordListID = Utils.BitConverter.ToUInt32(buffer, 0x48);
            this.numberOfRecords = Utils.BitConverter.ToUInt16(buffer, 0x4C);
        }

        public void Write(BinaryWriter writer)
        {
            writer.BaseStream.Seek(this.offset, SeekOrigin.Begin);


            Utils.BitConverter.LittleEndian = false;

            List<byte> output = new List<byte>();
            output.AddRange(title.Encode());
            output.AddRange(Utils.BitConverter.GetBytes(attributes));
            output.AddRange(Utils.BitConverter.GetBytes(version));
            output.AddRange(Utils.BitConverter.GetBytes(created));
            output.AddRange(Utils.BitConverter.GetBytes(modified));
            output.AddRange(Utils.BitConverter.GetBytes(backup));
            output.AddRange(Utils.BitConverter.GetBytes(modnum));
            output.AddRange(Utils.BitConverter.GetBytes(appInfoId));
            output.AddRange(Utils.BitConverter.GetBytes(sortInfoID));
            output.AddRange(type.Encode());
            output.AddRange(creator.Encode());
            output.AddRange(Utils.BitConverter.GetBytes(uniqueIDseed));
            output.AddRange(Utils.BitConverter.GetBytes(nextRecordListID));
            output.AddRange(Utils.BitConverter.GetBytes(numberOfRecords));

            writer.Write(output.ToArray());
        }

        public void Print()
        {
            Console.WriteLine($@"
            title: {title}
            attributes: {attributes}
            version: {version}
            created: {created}
            modified: {modified}
            backup: {backup}
            modnum: {modnum}
            appInfoId: {appInfoId}
            sortInfoID: {sortInfoID}
            type: {type}
            creator: {creator}
            uniqueIDseed: {uniqueIDseed}
            nextRecordListID: {nextRecordListID}
            numberOfRecords: {numberOfRecords}
            ");
        }
    }

    public class Records : Dictionary<uint, (uint, byte)> {
        /// <summary>
        /// Holds metadata records for mobi as {uid: (offset, attributes)}
        /// </summary>

        public const long offset = 0x4E;
        public int length;

        public Records() { }

        public void Parse(BinaryReader reader, int recordCount)
        {
            length = 0x8 * recordCount;

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            byte[] rawBuffer = reader.ReadBytes(0x8 * recordCount);

            Utils.BitConverter.LittleEndian = false;

            for (var i = 0; i < rawBuffer.Length; i+= 0x8)
            {
                uint offset = Utils.BitConverter.ToUInt32(rawBuffer, i);
                // These 4 bytes contain an unsigned 8-bit int *and* big-endian
                // 24-bit int for attributes and uid like this: [attr-uid-uid-uid]
                byte[] cmbo = rawBuffer.SubArray(i+0x4, 0x4);
                byte attributes = cmbo[0];
                cmbo[0] = 0;

                uint key = Utils.BitConverter.ToUInt32(cmbo, 0x0);

                this.Add(key, (offset, attributes));
            }
        }

        public void Write(BinaryWriter writer)
        {
            Utils.BitConverter.LittleEndian = false;

            List<byte> output = new List<byte>();
            foreach (var kv in this)
            {
                output.AddRange(Utils.BitConverter.GetBytes(kv.Value.Item1)); // offset
                output.Add(kv.Value.Item2); // attrs
                output.AddRange(Utils.BitConverter.GetBytes(kv.Key).SubArray(0x1, 0x3));

            }
            writer.Write(output.ToArray());
        }

        public void Print()
        {
            foreach (var kv in this)
            {
                Console.WriteLine($"{kv.Key}: ({kv.Value.Item1}, {kv.Value.Item2})");
            }

        }

    }

    public class PalmDOCHeader
    {
        public long offset;
        public readonly int length = 0x10;

        public ushort compression;
        public uint textLength;
        public ushort recordCount;
        public ushort recordSize;
        public ushort encryptionType;
        public ushort mysteryData;

        public byte[] buffer; // DEBUG

        /// <summary>
        /// Data contained in PalmDOCHeader portion of mobi.
        /// </summary>
        public PalmDOCHeader() { }

        public void Parse(BinaryReader reader)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            buffer = reader.ReadBytes(length);

            Utils.BitConverter.LittleEndian = false;

            compression = Utils.BitConverter.ToUInt16(buffer, 0x0);
            // Skip 0x2 unused bytes
            textLength = Utils.BitConverter.ToUInt32(buffer, 0x4);
            recordCount = Utils.BitConverter.ToUInt16(buffer, 0x8);
            recordSize = Utils.BitConverter.ToUInt16(buffer, 0xA);
            encryptionType = Utils.BitConverter.ToUInt16(buffer, 0xC);
            mysteryData = Utils.BitConverter.ToUInt16(buffer, 0xE);
        }

        public void Write(BinaryWriter writer)
        {
            writer.BaseStream.Seek(this.offset, SeekOrigin.Begin);

            Utils.BitConverter.LittleEndian = false;

            List<byte> output = new List<byte>();
            output.AddRange(Utils.BitConverter.GetBytes(compression));
            output.AddRange(new byte[0x2]);
            output.AddRange(Utils.BitConverter.GetBytes(textLength));
            output.AddRange(Utils.BitConverter.GetBytes(recordCount));
            output.AddRange(Utils.BitConverter.GetBytes(recordSize));
            output.AddRange(Utils.BitConverter.GetBytes(encryptionType));
            output.AddRange(Utils.BitConverter.GetBytes(mysteryData));

            writer.Write(output.ToArray());
        }

        public void Print()
        {
            Console.WriteLine($@"
            compression: {compression}
            textLength: {textLength}
            recordCount: {recordCount}
            recordSize: {recordSize}
            encryptionType: {encryptionType}
            mysteryData: {mysteryData}
            ");
        }
    }

    public class MobiHeader
    {
        public long offset;
        public int length = 0xC8;
        // ^ This is not the *actual* length, just the minum length to get
        //  all of the required information. After parsing this will be
        //  equal to headerLength

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
        public uint imageIndexOffset;
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
        public ushort lastImageRecord;
        public byte[] unknown3;
        public uint fcisRecord;
        public byte[] unknown4;
        public uint flisRecord;
        public byte[] unknown5;

        public bool hasDRM;
        public bool hasEXTH;
        public string fullTitle;

        /// <summary>
        /// Data contained in MobiHeader portion of mobi.
        /// All offsets are relative to 0x0
        /// </summary>
        public MobiHeader() { }

        public void Parse(BinaryReader reader, uint recordZeroOffset)
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
            fullTitleOffset = Utils.BitConverter.ToUInt32(buffer, 0x44) + recordZeroOffset;
            fullTitleLength = Utils.BitConverter.ToUInt32(buffer, 0x48);
            language = Utils.BitConverter.ToUInt32(buffer, 0x4C);
            inputLanguage = Utils.BitConverter.ToUInt32(buffer, 0x50);
            outputLanguage = Utils.BitConverter.ToUInt32(buffer, 0x54);
            formatVersion = Utils.BitConverter.ToUInt32(buffer, 0x58);
            imageIndexOffset = Utils.BitConverter.ToUInt32(buffer, 0x5C);
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
            unknown2 = buffer.SubArray(0xA8, 0xA);
            lastImageRecord = Utils.BitConverter.ToUInt16(buffer, 0xB2);
            unknown3 = buffer.SubArray(0xB4, 0x4);
            fcisRecord = Utils.BitConverter.ToUInt32(buffer, 0xB8);
            unknown4 = buffer.SubArray(0xBC, 0x4);
            flisRecord = Utils.BitConverter.ToUInt32(buffer, 0xC0);
            unknown5 = buffer.SubArray(0xC4, 0x4);

            hasDRM = drmOffset != 0xFFFFFFFF;
            hasEXTH = (exthFlags & 0x40) != 0;

            reader.BaseStream.Seek(fullTitleOffset, SeekOrigin.Begin);
            fullTitle = reader.ReadBytes((int)fullTitleLength).Decode();

            length = (int)headerLength;
        }

        public void Write(BinaryWriter writer, uint recordZeroOffset)
        {
            writer.BaseStream.Seek(this.offset, SeekOrigin.Begin);

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
            output.AddRange(Utils.BitConverter.GetBytes(fullTitleOffset - recordZeroOffset));
            output.AddRange(Utils.BitConverter.GetBytes(fullTitleLength));
            output.AddRange(Utils.BitConverter.GetBytes(language));
            output.AddRange(Utils.BitConverter.GetBytes(inputLanguage));
            output.AddRange(Utils.BitConverter.GetBytes(outputLanguage));
            output.AddRange(Utils.BitConverter.GetBytes(formatVersion));
            output.AddRange(Utils.BitConverter.GetBytes(imageIndexOffset));
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
            output.AddRange(Utils.BitConverter.GetBytes(lastImageRecord));
            output.AddRange(unknown3);
            output.AddRange(Utils.BitConverter.GetBytes(fcisRecord));
            output.AddRange(unknown4);
            output.AddRange(Utils.BitConverter.GetBytes(flisRecord));
            output.AddRange(unknown5);

            writer.Write(output.ToArray());
            Console.WriteLine(fullTitleOffset);
            writer.BaseStream.Seek(fullTitleOffset, SeekOrigin.Begin);
            writer.Write(fullTitle.Encode());
        }

        public void Print()
        {
            Console.WriteLine($@"
                offset: {offset}
                identifier: {identifier}
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
                imageIndexOffset: {imageIndexOffset}
                huffRecordOffset: {huffRecordOffset}
                huffRecordCount: {huffRecordCount}
                datpRecordOffset: {datpRecordOffset}
                datpRecordCount: {datpRecordCount}
                exthFlags: {exthFlags}
                drmOffset: {drmOffset}
                drmCount: {drmCount}
                drmSize: {drmSize}
                drmFlags: {drmFlags}
                lastImageRecord: {lastImageRecord}
                fcisRecord: {fcisRecord}
                flisRecord: {flisRecord}
            ");
        }
    }

    public class EXTHHeader : Dictionary<uint, byte[]>
    {
        public long offset;
        public int length;

        public uint identifier;
        public uint recordCount;

        public EXTHHeader() { }

        public enum RecordNames : uint
        {
            author = 100,
            publisher = 101,
            imprint = 102,
            description = 103,
            isbn = 104,
            subject = 105,
            pubdate = 106,
            review = 107,
            contributor = 108,
            rights = 109,
            subjectcode = 110,
            type = 111,
            source = 112,
            asin = 113,
            version = 114,
            sample = 115,
            startreading = 116
        }

        public void Parse(BinaryReader reader)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            byte[] buffer = reader.ReadBytes(0xC);

            Utils.BitConverter.LittleEndian = false;

            identifier = Utils.BitConverter.ToUInt32(buffer, 0x0);
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

        public void Write(BinaryWriter writer)
        {
            writer.BaseStream.Seek(offset, SeekOrigin.Begin);

            Utils.BitConverter.LittleEndian = false;

            List<byte> output = new List<Byte>();
            output.AddRange(Utils.BitConverter.GetBytes(identifier));
            output.AddRange(Utils.BitConverter.GetBytes((uint)length));
            output.AddRange(Utils.BitConverter.GetBytes((uint)this.Keys.Count));

            foreach (var kv in this)
            {
                output.AddRange(Utils.BitConverter.GetBytes(kv.Key)); // recType
                output.AddRange(Utils.BitConverter.GetBytes((uint)kv.Value.Length + 0x8));
                output.AddRange(kv.Value); // recData
            }
            writer.Write(output.ToArray());
        }

        public void Print()
        {
            Console.WriteLine($@"
offset: {offset}
length: {length}
identifier: {identifier}
recordCount: {recordCount}
            ");
            foreach (var item in this)
            {
                string d = string.Join(", ", item.Value);
                Console.WriteLine($"{item.Key}: {d}");
            }

        }

    }
}
