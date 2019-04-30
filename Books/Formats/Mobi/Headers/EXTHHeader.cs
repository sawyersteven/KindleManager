using System;
using System.Collections.Generic;
using System.IO;
using ExtensionMethods;

namespace Formats.Mobi.Headers
{
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
            foreach (uint k in this.Keys)
            {
                Console.WriteLine($"\t{k}: {Get(k)}");

            }
        }

    }
}
