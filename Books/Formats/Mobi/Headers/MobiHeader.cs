using System;
using System.Collections.Generic;
using System.IO;
using ExtensionMethods;

namespace Formats.Mobi.Headers
{
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
        public uint indxRecordNum;

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
                indxRecordNum = Utils.BitConverter.ToUInt32(reader.ReadBytes(0x4), 0x0);

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
    extraDataFlags: {extraDataFlags}
    indxRecordNum: {indxRecordNum}
            ");
        }
    }

}
