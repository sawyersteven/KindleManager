using System;
using System.Collections.Generic;
using System.IO;

namespace Formats.Mobi.Headers
{

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

}
