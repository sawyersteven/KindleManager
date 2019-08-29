using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.IO;

namespace Formats.Mobi.Headers
{
    public enum MobiHeaderType
    {
        Mobi6 = 6,
        Mobi8 = 8
    }

    public class MobiHeader
    {
        public readonly MobiHeaderType MobiType;

        public uint offset;

        #region parseable data
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
        public byte[] extraIndexes24Byte;
        public uint firstNonTextRecord;

        public uint fullTitleOffset;
        public uint fullTitleLength;
        public uint locale;
        public uint inputLanguage;

        public uint outputLanguage;
        public uint minVersion;
        public uint firstImageRecord;
        public uint huffRecordNum;

        public uint huffRecordCount;
        public uint huffTableOffset;
        public uint huffTableLength;
        public uint exthFlags;

        public byte[] filler32Byte;

        public uint unknown0;
        public uint drmOffset;
        public uint drmCount;
        public uint drmSize;

        public uint drmFlags;
        public byte[] filler8Byte;
        #region if mobi6
        public ushort firstContentRecord;
        public ushort lastContentRecord;
        #endregion
        #region if mobi8
        public uint fdstOffset;
        #endregion

        #region if mobi6
        public uint unknown1;
        #endregion
        #region if mobi8
        public uint fdstFlowCount;
        #endregion
        public uint fcisRecord;
        public uint fcisRecordCount;
        public uint flisRecord;

        public uint flisRecordCount;
        public uint unknown2;
        public uint unknown3;
        public uint firstSrcsRecord;

        public uint srcsRecordCount;
        #region if mobi6
        public uint unknown4;
        public uint unknown5;
        #endregion
        #region if mobi8
        public uint fragmentIndex;
        public uint skeletonIndex;
        #endregion
        public uint extraDataFlags;

        public uint ncxIndxRecord;
        public uint unknown6;
        public uint unknown7;
        public uint datpOffset;

        #region if mobi8
        public uint guideIndex;
        #endregion
        #endregion

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
        public MobiHeader(MobiHeaderType type)
        {
            MobiType = type;
            switch (MobiType)
            {
                case MobiHeaderType.Mobi6:
                    this.length = 232;
                    break;
                case MobiHeaderType.Mobi8:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Fills fields with a set of default values
        /// </summary>
        public void FillDefault()
        {
            identifier = "MOBI".Encode();

            mobiType = 2;
            textEncoding = 65001;

            uid = (uint)Utils.Metadata.RandomNumber();
            formatVersion = 6;
            orthoIndex = uint.MaxValue;
            inflectionIndex = uint.MaxValue;

            indexNames = uint.MaxValue;
            indexKeys = uint.MaxValue;
            extraIndexes24Byte = new byte[24];
            for (int i = 0; i < extraIndexes24Byte.Length; i++) extraIndexes24Byte[i] = 0xff;
            firstNonTextRecord = uint.MaxValue;

            fullTitleOffset = uint.MaxValue;
            fullTitleLength = uint.MaxValue;
            locale = 9;
            inputLanguage = 0;

            outputLanguage = 0;
            minVersion = 6;
            firstImageRecord = uint.MaxValue;
            huffRecordNum = 0;

            huffRecordCount = 0;
            huffTableOffset = 0;
            huffTableLength = 0;
            exthFlags = 0x50;

            filler32Byte = new byte[32];

            unknown0 = 0;
            drmOffset = uint.MaxValue;
            drmCount = 0;
            drmSize = 0;

            drmFlags = 0;
            filler8Byte = new byte[8];
            firstContentRecord = 1;
            lastContentRecord = ushort.MaxValue;
            fdstOffset = uint.MaxValue;

            unknown1 = 1;
            fdstFlowCount = 0;
            fcisRecord = 0;
            fcisRecordCount = 1;
            flisRecord = 0;

            flisRecordCount = 1;
            unknown2 = 0;
            unknown3 = 0;
            firstSrcsRecord = uint.MaxValue;

            srcsRecordCount = 0;
            unknown4 = 0;
            fragmentIndex = uint.MaxValue;
            unknown5 = 0;
            skeletonIndex = uint.MaxValue;
            extraDataFlags = 0;

            ncxIndxRecord = 0;
            unknown6 = 0;
            unknown7 = 0;
            datpOffset = uint.MaxValue;

            guideIndex = uint.MaxValue;

        }

        public void Parse(BinaryReader reader)
        {
            byte[] buffer;

            try
            {
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                buffer = reader.ReadBytes(0x8);
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to read Mobi header [0]: {e.Message}");
            }

            identifier = buffer.SubArray(0x0, 0x4);
            if (identifier.Decode() != "MOBI")
            {
                throw new FileFormatException($"Invalid Mobi header magic; Expected 'MOBI' found '{identifier.Decode()}'");
            }

            length = Utils.BigEndian.ToUInt32(buffer, 0x4);

            try
            {
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                buffer = reader.ReadBytes((int)length);
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to read Mobi header [1]: {e.Message}");
            }

            mobiType = Utils.BigEndian.ToUInt32(buffer, 0x8);
            textEncoding = Utils.BigEndian.ToUInt32(buffer, 0xC);

            uid = Utils.BigEndian.ToUInt32(buffer, 0x10);
            formatVersion = Utils.BigEndian.ToUInt32(buffer, 0x14);
            orthoIndex = Utils.BigEndian.ToUInt32(buffer, 0x18);
            inflectionIndex = Utils.BigEndian.ToUInt32(buffer, 0x1C);

            indexNames = Utils.BigEndian.ToUInt32(buffer, 0x20);
            indexKeys = Utils.BigEndian.ToUInt32(buffer, 0x24);
            extraIndexes24Byte = buffer.SubArray(0x28, 0x18);
            firstNonTextRecord = Utils.BigEndian.ToUInt32(buffer, 0x40);

            fullTitleOffset = Utils.BigEndian.ToUInt32(buffer, 0x44) + offset - 0x10;
            fullTitleLength = Utils.BigEndian.ToUInt32(buffer, 0x48);
            locale = Utils.BigEndian.ToUInt32(buffer, 0x4C);
            inputLanguage = Utils.BigEndian.ToUInt32(buffer, 0x50);

            outputLanguage = Utils.BigEndian.ToUInt32(buffer, 0x54);
            minVersion = Utils.BigEndian.ToUInt32(buffer, 0x58);
            firstImageRecord = Utils.BigEndian.ToUInt32(buffer, 0x5C);
            huffRecordNum = Utils.BigEndian.ToUInt32(buffer, 0x60);

            huffRecordCount = Utils.BigEndian.ToUInt32(buffer, 0x64);
            huffTableOffset = Utils.BigEndian.ToUInt32(buffer, 0x68);
            huffTableLength = Utils.BigEndian.ToUInt32(buffer, 0x6C);
            exthFlags = Utils.BigEndian.ToUInt32(buffer, 0x70);

            filler32Byte = buffer.SubArray(0x74, 32);

            unknown0 = Utils.BigEndian.ToUInt32(buffer, 0x94);
            drmOffset = Utils.BigEndian.ToUInt32(buffer, 0x98);
            drmCount = Utils.BigEndian.ToUInt32(buffer, 0x9C);
            drmSize = Utils.BigEndian.ToUInt32(buffer, 0xA0);

            drmFlags = Utils.BigEndian.ToUInt32(buffer, 0xA4);
            filler8Byte = buffer.SubArray(0xA8, 4);
            if (MobiType == MobiHeaderType.Mobi6)
            {
                firstContentRecord = Utils.BigEndian.ToUInt16(buffer, 0xB0);
                lastContentRecord = Utils.BigEndian.ToUInt16(buffer, 0xB2);
            }
            else
            {
                fdstOffset = Utils.BigEndian.ToUInt32(buffer, 0xB0);
            }

            if (MobiType == MobiHeaderType.Mobi6)
            {
                unknown1 = Utils.BigEndian.ToUInt32(buffer, 0xB4);
            }
            else
            {
                fdstFlowCount = Utils.BigEndian.ToUInt32(buffer, 0xB4);
            }
            fcisRecord = Utils.BigEndian.ToUInt32(buffer, 0xB8);
            fcisRecordCount = Utils.BigEndian.ToUInt32(buffer, 0xBC);
            flisRecord = Utils.BigEndian.ToUInt32(buffer, 0xC0);

            flisRecordCount = Utils.BigEndian.ToUInt32(buffer, 0xC4);
            unknown2 = Utils.BigEndian.ToUInt32(buffer, 0xC8);
            unknown3 = Utils.BigEndian.ToUInt32(buffer, 0xCC);
            firstSrcsRecord = Utils.BigEndian.ToUInt32(buffer, 0xD0);

            srcsRecordCount = Utils.BigEndian.ToUInt32(buffer, 0xD4);
            if (MobiType == MobiHeaderType.Mobi6)
            {
                unknown4 = Utils.BigEndian.ToUInt32(buffer, 0xD8);
                unknown5 = Utils.BigEndian.ToUInt32(buffer, 0xDC);
            }
            else
            {
                fragmentIndex = Utils.BigEndian.ToUInt32(buffer, 0xD8);
                skeletonIndex = Utils.BigEndian.ToUInt32(buffer, 0xDC);
            }
            extraDataFlags = Utils.BigEndian.ToUInt32(buffer, 0xE0);

            ncxIndxRecord = Utils.BigEndian.ToUInt32(buffer, 0xE4);

            if (length > 0xe8)
            {
                unknown6 = Utils.BigEndian.ToUInt32(buffer, 0xE8);
                unknown7 = Utils.BigEndian.ToUInt32(buffer, 0xEC);
                datpOffset = Utils.BigEndian.ToUInt32(buffer, 0xD0);

                if (MobiType == MobiHeaderType.Mobi8)
                {
                    guideIndex = Utils.BigEndian.ToUInt32(buffer, 0xD4);
                }
            }

            hasDRM = drmOffset != 0xFFFFFFFF;
            hasEXTH = (exthFlags & 0x40) != 0;

            (flagMultiByte, flagTrailingEntries) = DecodeExtraDataFlags(extraDataFlags);

            try
            {
                reader.BaseStream.Seek(fullTitleOffset, SeekOrigin.Begin);
                fullTitle = reader.ReadBytes((int)fullTitleLength).Decode();
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to read full title from Mobi header: {e.Message}");
            }
        }

        private ValueTuple<bool, uint> DecodeExtraDataFlags(uint extraDataFlags)
        {
            flagMultiByte = (extraDataFlags & 0x01) == 1;
            extraDataFlags >>= 0x01;
            while (extraDataFlags > 0)
            {
                flagTrailingEntries += extraDataFlags & 0x01;
                extraDataFlags >>= 0x01;
            }
            return new ValueTuple<bool, uint>(flagMultiByte, flagTrailingEntries);
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
            output.AddRange(extraIndexes24Byte);
            output.AddRange(Utils.BigEndian.GetBytes(firstNonTextRecord));

            output.AddRange(Utils.BigEndian.GetBytes(fullTitleOffset - offset + 0x10));
            output.AddRange(Utils.BigEndian.GetBytes(fullTitleLength));
            output.AddRange(Utils.BigEndian.GetBytes(locale));
            output.AddRange(Utils.BigEndian.GetBytes(inputLanguage));

            output.AddRange(Utils.BigEndian.GetBytes(outputLanguage));
            output.AddRange(Utils.BigEndian.GetBytes(minVersion));
            output.AddRange(Utils.BigEndian.GetBytes(firstImageRecord));
            output.AddRange(Utils.BigEndian.GetBytes(huffRecordNum));

            output.AddRange(Utils.BigEndian.GetBytes(huffRecordCount));
            output.AddRange(Utils.BigEndian.GetBytes(huffTableOffset));
            output.AddRange(Utils.BigEndian.GetBytes(huffTableLength));
            output.AddRange(Utils.BigEndian.GetBytes(exthFlags));

            output.AddRange(filler32Byte);

            output.AddRange(Utils.BigEndian.GetBytes(unknown0));
            output.AddRange(Utils.BigEndian.GetBytes(drmOffset));
            output.AddRange(Utils.BigEndian.GetBytes(drmCount));
            output.AddRange(Utils.BigEndian.GetBytes(drmSize));

            output.AddRange(Utils.BigEndian.GetBytes(drmFlags));
            output.AddRange(filler8Byte);
            if (MobiType == MobiHeaderType.Mobi6)
            {
                output.AddRange(Utils.BigEndian.GetBytes(firstContentRecord));
                output.AddRange(Utils.BigEndian.GetBytes(lastContentRecord));
            }
            else
            {
                output.AddRange(Utils.BigEndian.GetBytes(fdstOffset));
            }

            if (MobiType == MobiHeaderType.Mobi6)
            {
                output.AddRange(Utils.BigEndian.GetBytes(unknown1));
            }
            else
            {
                output.AddRange(Utils.BigEndian.GetBytes(fdstFlowCount));
            }
            output.AddRange(Utils.BigEndian.GetBytes(fcisRecord));
            output.AddRange(Utils.BigEndian.GetBytes(fcisRecordCount));
            output.AddRange(Utils.BigEndian.GetBytes(flisRecord));

            output.AddRange(Utils.BigEndian.GetBytes(flisRecordCount));
            output.AddRange(Utils.BigEndian.GetBytes(unknown2));
            output.AddRange(Utils.BigEndian.GetBytes(unknown3));
            output.AddRange(Utils.BigEndian.GetBytes(firstSrcsRecord));

            output.AddRange(Utils.BigEndian.GetBytes(srcsRecordCount));
            if (MobiType == MobiHeaderType.Mobi6)
            {
                output.AddRange(Utils.BigEndian.GetBytes(unknown4));
                output.AddRange(Utils.BigEndian.GetBytes(unknown5));
            }
            else
            {
                output.AddRange(Utils.BigEndian.GetBytes(fragmentIndex));
                output.AddRange(Utils.BigEndian.GetBytes(skeletonIndex));
            }
            output.AddRange(Utils.BigEndian.GetBytes(extraDataFlags));

            output.AddRange(Utils.BigEndian.GetBytes(ncxIndxRecord));

            if (MobiType == MobiHeaderType.Mobi8)
            {
                output.AddRange(Utils.BigEndian.GetBytes(unknown6));
                output.AddRange(Utils.BigEndian.GetBytes(unknown7));
                output.AddRange(Utils.BigEndian.GetBytes(datpOffset));
                output.AddRange(Utils.BigEndian.GetBytes(guideIndex));
            }

            output.AddRange(new byte[(int)length - output.Count]);

            return output.ToArray();
        }

        public void Write(BinaryWriter writer)
        {
            try
            {
                writer.BaseStream.Seek(offset, SeekOrigin.Begin);
                writer.Write(Dump());
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to write Mobi header to file: {e.Message}");
            }
        }

        public void WriteTitle(BinaryWriter writer)
        {
            try
            {
                writer.BaseStream.Seek(fullTitleOffset, SeekOrigin.Begin);
                writer.Write(fullTitle.Encode());
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to write title after headers: {e.Message}");
            }
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
    firstNonTextRecord: {firstNonTextRecord}
    fullTitleOffset: {fullTitleOffset}
    fullTitleLength: {fullTitleLength}
    locale: {locale}
    inputLanguage: {inputLanguage}
    outputLanguage: {outputLanguage}
    minVersion: {minVersion}
    firstImageRecord: {firstImageRecord}
    huffRecordNum: {huffRecordNum}
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
    firstCompliationData: {firstSrcsRecord}
    compilationDataCount: {srcsRecordCount}
    extraDataFlags: {extraDataFlags}
    indxRecord: {ncxIndxRecord}
    ncxIndxRecord: {ncxIndxRecord}
            ");
        }
    }

}
