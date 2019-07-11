using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.IO;

namespace Formats.Mobi.Headers
{
    public enum EXTHRecordID
    {
        Author = 100,
        Publisher = 101,
        Imprint = 102,
        Description = 103,
        ISBN = 104,
        Subject = 105,
        PublishDate = 106,
        Review = 107,
        Contributor = 108,
        Rights = 109,
        SubjectCode = 110,
        Type = 111,
        Source = 112,
        ASIN = 113,
        VersionNumber = 114,
        IsSample = 115,
        StartReading = 116,
        RetailPrice = 118,
        RetailPriceCurrency = 119,
        DictShortName = 200,
        Creator = 204,
        CDEType = 501,
        UpdatedTitle = 503,
        ASIN2 = 504,
        Language = 524,
    }

    public class EXTHHeader : Dictionary<uint, byte[]>
    {
        public long offset;
        public int length
        {
            get
            {
                int l = 12 + (8 * this.Keys.Count);
                foreach (byte[] v in this.Values)
                {
                    l += v.Length;
                }
                return l + (l % 4);
            }
        }

        public byte[] identifier;
        public uint recordCount
        {
            get => (uint)this.Keys.Count;
        }

        public EXTHHeader() { }

        public void Set(EXTHRecordID rec, byte[] val)
        {
            this[(uint)rec] = val;
        }

        public void Set(uint rec, byte[] val)
        {
            this[rec] = val;
        }

        public byte[] Get(EXTHRecordID rec)
        {
            TryGetValue((uint)rec, out byte[] val);
            return val ?? new byte[0];
        }

        public byte[] Get(uint rec)
        {
            TryGetValue(rec, out byte[] val);
            return val ?? new byte[0];
        }

        public void Parse(BinaryReader reader)
        {
            byte[] buffer;

            try
            {
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                buffer = reader.ReadBytes(0xC);
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to read EXTH header: {e.Message}");
            }

            identifier = buffer.SubArray(0x0, 0x4);
            if (identifier.Decode() != "EXTH")
            {
                throw new FileFormatException($"Invalid EXTH header; Expected 'EXTH', found '{identifier.Decode()}'");
            }
            uint _recordCount = Utils.BigEndian.ToUInt32(buffer, 0x8);

            for (int i = 0; i < _recordCount; i++)
            {
                try
                {
                    buffer = reader.ReadBytes(0x8);
                    uint recType = Utils.BigEndian.ToUInt32(buffer, 0x0);
                    int recLen = Utils.BigEndian.ToInt32(buffer, 0x4) - 0x8;
                    byte[] recData = reader.ReadBytes(recLen);
                    if (!this.ContainsKey(recType)) this.Add(recType, recData);
                }
                catch (Exception e)
                {
                    throw new Exception($"Unable to read EXTH header record [{i}]: {e.Message}");
                }
            }
        }

        public byte[] Dump()
        {
            List<byte> records = new List<byte>();
            foreach (var kv in this)
            {
                records.AddRange(Utils.BigEndian.GetBytes(kv.Key));                     // recType
                records.AddRange(Utils.BigEndian.GetBytes((uint)kv.Value.Length + 0x8));
                records.AddRange(kv.Value);                                             // recData
            }

            List<byte> output = new List<byte>();
            output.AddRange(identifier);
            output.AddRange(Utils.BigEndian.GetBytes((uint)length));
            output.AddRange(Utils.BigEndian.GetBytes((uint)Keys.Count));
            output.AddRange(records);

            output.AddRange(new byte[output.Count % 4]); // pad to 4-byte multiple

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
                throw new Exception($"Unable to write EXTH header to file: {e.Message}");
            }
        }

        public void Print()
        {
            Console.WriteLine($@"
EXTH HEADER:
    identifier: {identifier.Decode()}
    recordCount: {recordCount}
            ");
            foreach (uint k in this.Keys)
            {
                Console.WriteLine($"\t{k}: {Get(k).Decode()}");
            }
        }

    }
}
