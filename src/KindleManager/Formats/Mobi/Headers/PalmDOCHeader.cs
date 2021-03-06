﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Formats.Mobi.Headers
{

    public class PalmDOCHeader
    {
        public uint offset;
        public readonly uint length = 0x10;

        public ushort compression;
        public uint textLength;
        public ushort textRecordCount;
        public ushort recordSize;
        public ushort encryptionType;
        public ushort unknown;

        /// <summary>
        /// Data contained in PalmDOCHeader portion of mobi.
        /// </summary>
        public PalmDOCHeader() { }

        /// <summary>
        /// Fills fields with a set of default values
        /// </summary>
        /// <remarks>
        /// Set to no compression, no encryption
        /// </remarks>
        public void FillDefault()
        {
            compression = 1;
            textLength = 0;
            textRecordCount = 0;
            recordSize = 4096;
            encryptionType = 0;
            unknown = 0;
        }

        public void Parse(BinaryReader reader)
        {
            byte[] buffer;
            try
            {
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                buffer = reader.ReadBytes((int)length);
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to read PalmDoc header: {e.Message}");
            }

            compression = Utils.BigEndian.ToUInt16(buffer, 0x0);
            // Skip 0x2 unused bytes
            textLength = Utils.BigEndian.ToUInt32(buffer, 0x4);
            textRecordCount = Utils.BigEndian.ToUInt16(buffer, 0x8);
            recordSize = Utils.BigEndian.ToUInt16(buffer, 0xA);
            encryptionType = Utils.BigEndian.ToUInt16(buffer, 0xC);
            unknown = Utils.BigEndian.ToUInt16(buffer, 0xE);
        }

        public byte[] Dump()
        {
            List<byte> output = new List<byte>();
            output.AddRange(Utils.BigEndian.GetBytes(compression));
            output.AddRange(new byte[0x2]);
            output.AddRange(Utils.BigEndian.GetBytes(textLength));
            output.AddRange(Utils.BigEndian.GetBytes(textRecordCount));
            output.AddRange(Utils.BigEndian.GetBytes(recordSize));
            output.AddRange(Utils.BigEndian.GetBytes(encryptionType));
            output.AddRange(Utils.BigEndian.GetBytes(unknown));
            return output.ToArray();
        }

        public void Write(BinaryWriter writer)
        {
            try
            {
                writer.BaseStream.Seek(this.offset, SeekOrigin.Begin);
                writer.Write(Dump());
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to write PalmDOC header to file: ${e.Message}");
            }
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
    unknown: {unknown}
            ");
        }
    }

}
