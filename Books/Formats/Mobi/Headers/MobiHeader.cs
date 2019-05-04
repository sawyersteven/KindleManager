using System;
using System.Collections.Generic;
using System.IO;
using ExtensionMethods;

namespace Formats.Mobi.Headers
{
    public class MobiHeader
    {
        public uint offset;

        public byte[] identifier;
        public uint length;
        public uint mobiType;
        public uint textEncoding;
        public uint uid;
        public uint formatVersion;
        public uint orthoIndex;
        public uint inflectionIndex;
        public uint indexNames;
        public uint indexKeys;
        public byte[] extraIndexes;
        public uint firstNonBookRecord;
        public uint fullTitleOffset;
        public uint fullTitleLength;
        public uint locale;
        public uint inputLanguage;
        public uint outputLanguage;
        public uint minVersion;
        public uint firstImageRecord;
        public uint huffRecordOffset;
        public uint huffRecordCount;
        public uint huffTableOffset;
        public uint huffTableLength;
        public uint exthFlags;
        public byte[] unknown1;
        public uint drmOffset;
        public uint drmCount;
        public uint drmSize;
        public uint drmFlags;
        public ulong unknown2;
        public ushort firstContentRecord;
        public ushort lastContentRecord;
        public uint unknown3;
        public uint fcisRecord;
        public uint fcisRecordCount;
        public uint flisRecord;
        public uint flisRecordCount;
        public ulong unknown4;
        public uint unknown5;
        public uint firstCompilationData;
        public uint compilationDataCount;
        public uint unknown6;

        // This only exists if the headerlength is 0xe4 or 0xe8
        public uint extraDataFlags;
        public uint indxRecord;
        //

        public bool hasEXTH;
        public bool flagMultiByte;
        public uint flagTrailingEntries;
        public bool hasDRM;

        private string _fullTitle;
        public string fullTitle
        {
            get => _fullTitle;
            set
            {
                fullTitleLength = (uint)value.Encode().Length;
                _fullTitle = value;
            }
        }
        

        /// <summary>
        /// Data contained in MobiHeader portion of mobi.
        /// All offsets are relative to 0x0
        /// </summary>
        public MobiHeader() { }

        /// <summary>
        /// Fills fields with a set of default values
        /// </summary>
        public void FillDefault()
        {
            identifier = "MOBI".Encode();
            length = 0xe8;
            mobiType = 2;
            textEncoding = 65001;

            uid = (uint)Utils.Metadata.RandomNumber();
            formatVersion = 6;
            orthoIndex = 0xFFFFFFFF;
            inflectionIndex = 0xFFFFFFFF;

            indexNames = 0xFFFFFFFF;
            indexKeys = 0xFFFFFFFF;
            extraIndexes = new byte[0x18];
            for (int i = 0; i < extraIndexes.Length; i++) extraIndexes[i] = 0xff;

            firstNonBookRecord = 0xFFFFFFFF;
            fullTitleOffset = 0xFFFFFFFF;
            fullTitleLength = 0xFFFFFFFF;
            locale = 9;

            inputLanguage = 0;
            outputLanguage = 0;
            minVersion = 6;
            firstImageRecord = 0xFFFFFFFF;

            huffRecordOffset = 0;
            huffRecordCount = 0;
            huffTableOffset = 0;
            huffTableLength = 0;

            exthFlags = 0x50;
            unknown1 = new byte[36];
            drmOffset = 0xFFFFFFFF;
            drmCount = 0;

            drmSize = 0;
            drmFlags = 0;
            unknown2 = 0x0000000000000000;

            firstContentRecord = 1;
            lastContentRecord = 0xFFFF;
            unknown3 = 0x00000001;
            fcisRecord = 0;
            fcisRecordCount = 1;

            flisRecord = 0;
            flisRecordCount = 1;
            unknown4 = 0x0000000000000000;

            unknown5 = 0xFFFFFFFF;
            firstCompilationData = 0x00000000;
            compilationDataCount = 0xFFFFFFFF;
            unknown6 = 0xFFFFFFFF;

            extraDataFlags = 0;
            indxRecord = 0;
        }

        public void Parse(BinaryReader reader)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            byte[] buffer = reader.ReadBytes(0x8);

            identifier = buffer.SubArray(0x0, 0x4); // MOBI
            length = Utils.BigEndian.ToUInt32(buffer, 0x4);

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            buffer = reader.ReadBytes((int)length);

            mobiType = Utils.BigEndian.ToUInt32(buffer, 0x8);
            textEncoding = Utils.BigEndian.ToUInt32(buffer, 0xC);
            uid = Utils.BigEndian.ToUInt32(buffer, 0x10);
            formatVersion = Utils.BigEndian.ToUInt32(buffer, 0x14);
            orthoIndex = Utils.BigEndian.ToUInt32(buffer, 0x18);
            inflectionIndex = Utils.BigEndian.ToUInt32(buffer, 0x1C);
            indexNames = Utils.BigEndian.ToUInt32(buffer, 0x20);
            indexKeys = Utils.BigEndian.ToUInt32(buffer, 0x24);
            extraIndexes = buffer.SubArray(0x28, 0x18);
            firstNonBookRecord = Utils.BigEndian.ToUInt32(buffer, 0x40);
            fullTitleOffset = Utils.BigEndian.ToUInt32(buffer, 0x44) + offset - 0x10; // Offset is from PDBHeader table, or 0x10.
            fullTitleLength = Utils.BigEndian.ToUInt32(buffer, 0x48);
            locale = Utils.BigEndian.ToUInt32(buffer, 0x4C);
            inputLanguage = Utils.BigEndian.ToUInt32(buffer, 0x50);
            outputLanguage = Utils.BigEndian.ToUInt32(buffer, 0x54);
            minVersion = Utils.BigEndian.ToUInt32(buffer, 0x58);
            firstImageRecord = Utils.BigEndian.ToUInt32(buffer, 0x5C);
            huffRecordOffset = Utils.BigEndian.ToUInt32(buffer, 0x60);
            huffRecordCount = Utils.BigEndian.ToUInt32(buffer, 0x64);
            huffTableOffset = Utils.BigEndian.ToUInt32(buffer, 0x68);
            huffTableLength = Utils.BigEndian.ToUInt32(buffer, 0x6C);
            exthFlags = Utils.BigEndian.ToUInt32(buffer, 0x70);
            unknown1 = buffer.SubArray(0x74, 0x24);
            drmOffset = Utils.BigEndian.ToUInt32(buffer, 0x98);
            drmCount = Utils.BigEndian.ToUInt32(buffer, 0x9C);
            drmSize = Utils.BigEndian.ToUInt32(buffer, 0xA0);
            drmFlags = Utils.BigEndian.ToUInt32(buffer, 0xA4);
            unknown2 = Utils.BigEndian.ToUInt64(buffer, 0xA8);
            firstContentRecord = Utils.BigEndian.ToUInt16(buffer, 0xB0);
            lastContentRecord = Utils.BigEndian.ToUInt16(buffer, 0xB2);
            unknown3 = Utils.BigEndian.ToUInt32(buffer, 0xB4);
            fcisRecord = Utils.BigEndian.ToUInt32(buffer, 0xB8);
            fcisRecordCount = Utils.BigEndian.ToUInt32(buffer, 0xBC);
            flisRecord = Utils.BigEndian.ToUInt32(buffer, 0xC0);
            flisRecordCount = Utils.BigEndian.ToUInt32(buffer, 0xC4);
            unknown4 = Utils.BigEndian.ToUInt64(buffer, 0xC8);
            unknown5 = Utils.BigEndian.ToUInt32(buffer, 0xD0);
            firstCompilationData = Utils.BigEndian.ToUInt32(buffer, 0xD4);
            compilationDataCount = Utils.BigEndian.ToUInt32(buffer, 0xD8);
            unknown6 = Utils.BigEndian.ToUInt32(buffer, 0xDC);

            if (length >= 0xe4)
            {
                extraDataFlags = Utils.BigEndian.ToUInt32(buffer, 0xe0);
                uint edf = extraDataFlags;
                flagMultiByte = (edf & 0x01) == 1;
                edf >>= 0x01;
                while (edf > 0)
                {
                    flagTrailingEntries += edf & 0x01;
                    edf >>= 0x01;
                }
            }

            if (length >= 0xe8)
            {
                indxRecord = Utils.BigEndian.ToUInt32(buffer, 0xe4);
            }


            reader.BaseStream.Seek(fullTitleOffset, SeekOrigin.Begin);
            fullTitle = reader.ReadBytes((int)fullTitleLength).Decode();

            hasDRM = drmOffset != 0xFFFFFFFF;
            hasEXTH = (exthFlags & 0x40) != 0;
        }

        /// <summary>
        /// Serializes header into byte array
        /// Always produces longest header possible according to MobileRead chart
        /// </summary>
        public byte[] Dump()
        {
            List<byte> output = new List<byte>();

            output.AddRange(identifier);
            output.AddRange(Utils.BigEndian.GetBytes(length));
            output.AddRange(Utils.BigEndian.GetBytes(mobiType));
            output.AddRange(Utils.BigEndian.GetBytes(textEncoding));
            output.AddRange(Utils.BigEndian.GetBytes(uid));

            output.AddRange(Utils.BigEndian.GetBytes(formatVersion));
            output.AddRange(Utils.BigEndian.GetBytes(orthoIndex));
            output.AddRange(Utils.BigEndian.GetBytes(inflectionIndex));
            output.AddRange(Utils.BigEndian.GetBytes(indexNames));

            output.AddRange(Utils.BigEndian.GetBytes(indexKeys));
            output.AddRange(extraIndexes);

            output.AddRange(Utils.BigEndian.GetBytes(firstNonBookRecord));
            output.AddRange(Utils.BigEndian.GetBytes(fullTitleOffset - offset + 0x10));
            output.AddRange(Utils.BigEndian.GetBytes(fullTitleLength));
            output.AddRange(Utils.BigEndian.GetBytes(locale));

            output.AddRange(Utils.BigEndian.GetBytes(inputLanguage));
            output.AddRange(Utils.BigEndian.GetBytes(outputLanguage));
            output.AddRange(Utils.BigEndian.GetBytes(minVersion));
            output.AddRange(Utils.BigEndian.GetBytes(firstImageRecord));

            output.AddRange(Utils.BigEndian.GetBytes(huffRecordOffset));
            output.AddRange(Utils.BigEndian.GetBytes(huffRecordCount));
            output.AddRange(Utils.BigEndian.GetBytes(huffTableOffset));
            output.AddRange(Utils.BigEndian.GetBytes(huffTableLength));

            output.AddRange(Utils.BigEndian.GetBytes(exthFlags));
            output.AddRange(unknown1);
            output.AddRange(Utils.BigEndian.GetBytes(drmOffset));
            output.AddRange(Utils.BigEndian.GetBytes(drmCount));

            output.AddRange(Utils.BigEndian.GetBytes(drmSize));
            output.AddRange(Utils.BigEndian.GetBytes(drmFlags));
            output.AddRange(Utils.BigEndian.GetBytes(unknown2));

            output.AddRange(Utils.BigEndian.GetBytes(firstContentRecord));
            output.AddRange(Utils.BigEndian.GetBytes(lastContentRecord));
            output.AddRange(Utils.BigEndian.GetBytes(unknown3));
            output.AddRange(Utils.BigEndian.GetBytes(fcisRecord));
            output.AddRange(Utils.BigEndian.GetBytes(fcisRecordCount));

            output.AddRange(Utils.BigEndian.GetBytes(flisRecord));
            output.AddRange(Utils.BigEndian.GetBytes(flisRecordCount));
            output.AddRange(Utils.BigEndian.GetBytes(unknown4));

            output.AddRange(Utils.BigEndian.GetBytes(unknown5));
            output.AddRange(Utils.BigEndian.GetBytes(firstCompilationData));
            output.AddRange(Utils.BigEndian.GetBytes(compilationDataCount));
            output.AddRange(Utils.BigEndian.GetBytes(unknown6));

            output.AddRange(Utils.BigEndian.GetBytes(extraDataFlags));
            output.AddRange(Utils.BigEndian.GetBytes(indxRecord));

            return output.ToArray();
        }

        public void Write(BinaryWriter writer, bool seekToOffset = true)
        {
            if (seekToOffset)
            {
                writer.BaseStream.Seek(offset, SeekOrigin.Begin);
            }
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
    length: {length}
    mobiType: {mobiType}
    textEncoding: {textEncoding}
    uid: {uid}
    generatorVersion: {formatVersion}
    orthoIndex: {orthoIndex}
    inflectionIndex: {inflectionIndex}
    indexNames: {indexNames}
    indexKeys: {indexKeys}
    firstNonBookRecord: {firstNonBookRecord}
    fullTitleOffset: {fullTitleOffset}
    fullTitleLength: {fullTitleLength}
    locale: {locale}
    inputLanguage: {inputLanguage}
    outputLanguage: {outputLanguage}
    minVersion: {minVersion}
    firstImageRecord: {firstImageRecord}
    huffRecordOffset: {huffRecordOffset}
    huffRecordCount: {huffRecordCount}
    huffTableOffset: {huffTableOffset}
    huffTableLength: {huffTableLength}
    exthFlags: {exthFlags}
    drmOffset: {drmOffset}
    drmCount: {drmCount}
    drmSize: {drmSize}
    drmFlags: {drmFlags}
    firstContentRecord: {firstContentRecord}
    lastContentRecord: {lastContentRecord}
    fcisRecord: {fcisRecord}
    fcisRecordCount: {fcisRecordCount}
    flisRecord: {flisRecord}
    flisRecordCount: {flisRecordCount}
    firstCompliationData: {firstCompilationData}
    compilationDataCount: {compilationDataCount}
    extraDataFlags: {extraDataFlags}
    indxRecord: {indxRecord}
            ");
        }
    }

}
