using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils.Decompressors
{
    class HuffCdic : IDecompressor
    {
        private List<uint> mincode;
        private List<uint> maxcode;

        public HuffCdic(byte[] huffRecord)
        {
            if (!huffRecord.SubArray(0, 0x8).SequenceEqual(new byte[] { 0x48, 0x55, 0x46, 0x46, 0x00, 0x00, 0x00, 0x18 }))
            {
                throw new ArgumentException("HUFF record header invalid");
            }
            uint offs1 = BigEndian.ToUInt32(huffRecord, 0x8);
            uint offs2 = BigEndian.ToUInt32(huffRecord, 0xC);

            (uint, uint, uint)[] dict1 = new (uint, uint, uint)[256];
            for (int i = 0; i < 256; i++)
            {
                dict1[i] = DictUnpack(BigEndian.ToUInt32(huffRecord, (int)offs1 + (4 * i)));
            }

            uint[] dict2 = new uint[64];
            for (int i = 0; i < 64; i++)
            {
                dict2[i] = BigEndian.ToUInt32(huffRecord, (int)offs2 + (4 * i));
            }

            mincode = new List<uint>();
            mincode.Add(0);
            for (int i = 0; i < dict2.Length; i += 2)
            {
                mincode.Add(dict2[i] << (32 - i + 1));
            }

            maxcode = new List<uint>();
            maxcode.Add(0);
            for (int i = 1; i < dict2.Length; i += 2)
            {
                mincode.Add(dict2[i] << (32 - i + 1));
            }

        }


        private (uint, uint, uint) DictUnpack(uint v)
        {
            uint len = v & 0x1f;
            uint term = v & 0x80;
            uint max = v >> 8;

            if (len == 0 || term == 0)
            {
                throw new ArgumentException("HUFF dictionary is invalid");
            }

            return (len, term, ((max + 1) << (int)(32 - len)) - 1);
        }


        public byte[] Decompress(byte[] buffer)
        {
            List<byte> output = new List<byte>();


            return output.ToArray();
        }

    }
}
