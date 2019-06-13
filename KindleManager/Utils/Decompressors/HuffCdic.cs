using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils.Decompressors
{
    class HuffCdic : IDecompressor
    {
        private List<uint> mincode = new List<uint>();
        private List<uint> maxcode = new List<uint>();

        private List<(byte[], int)> dict = new List<(byte[], int)>();

        (uint, uint, uint)[] dict1 = new (uint, uint, uint)[256];

        public HuffCdic(byte[][] huffRecords)
        {
            ReadHuffRecord(huffRecords[0]);
            foreach (var rec in huffRecords.SubArray(1, -1))
            {
                ReadCdicRecord(rec);
            }
        }

        private void ReadHuffRecord(byte[] huffRecord)
        {
            if (!huffRecord.SubArray(0, 0x8).SequenceEqual(new byte[] { 0x48, 0x55, 0x46, 0x46, 0x00, 0x00, 0x00, 0x18 }))
            {
                throw new ArgumentException("HUFF record header invalid");
            }
            uint offs1 = BigEndian.ToUInt32(huffRecord, 0x8);
            uint offs2 = BigEndian.ToUInt32(huffRecord, 0xC);

            for (int i = 0; i < 256; i++)
            {
                dict1[i] = DictUnpack(BigEndian.ToUInt32(huffRecord, (int)offs1 + (4 * i)));
            }

            uint[] dict2 = new uint[64];
            for (int i = 0; i < 64; i++)
            {
                dict2[i] = BigEndian.ToUInt32(huffRecord, (int)offs2 + (4 * i));
            }

            mincode.Add(0);
            maxcode.Add(uint.MaxValue);

            for (int i = 0; i < dict2.Length; i += 2)
            {
                mincode.Add(dict2[i] << (32 - ((i / 2) + 1)));
                int j = (i / 2) + 1;
                maxcode.Add(((dict2[i + 1] + 1) << (32 - j)) - 1);
            }

        }

        private void ReadCdicRecord(byte[] cdicRecord)
        {
            int phrases = (int)BigEndian.ToUInt32(cdicRecord, 0x8);
            int bits = (int)BigEndian.ToUInt32(cdicRecord, 0xc);

            int n = Math.Min(1 << bits, phrases - dict.Count);
            for (int i = 0; i < n; i++)
            {
                ushort offs = BigEndian.ToUInt16(cdicRecord, 0x10 + (i * 2));
                ushort blen = BigEndian.ToUInt16(cdicRecord, 0x10 + offs);
                var slice = cdicRecord.SubArray(18 + offs, blen & 0x7fff);
                dict.Add((slice, blen & 0x8000));
            }
        }

        // TODO first cdic for Miniatures is ok, second is not

        private (uint, uint, uint) DictUnpack(uint v)
        {
            uint len = v & 0x1f;
            uint term = v & 0x80;
            uint max = v >> 8;

            if (len == 0 || (len <= 8 && term == 0))
            {
                throw new ArgumentException("HUFF dictionary is invalid");
            }

            return (len, term, ((max + 1) << (int)(32 - len)) - 1);
        }

        public byte[] Decompress(byte[] buffer)
        {
            List<byte> output = new List<byte>();

            int remaining = buffer.Length * 8;

            buffer = buffer.Append(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            int pos = 0;

            ulong x = BigEndian.ToUInt64(buffer, pos);
            int n = 32;

            while (true)
            {
                if (n <= 0)
                {
                    pos += 4;
                    x = BigEndian.ToUInt64(buffer, pos);
                    n += 32;
                }
                var code = (x >> n) & ((1 << 32) - 1);

                (uint len, uint term, uint max) = dict1[code >> 24];

                if (term == 0)
                {
                    while (code < mincode[(int)len])
                    {
                        len += 1;
                    }
                    max = maxcode[(int)len];
                }

                n -= (int)len;
                remaining -= (int)len;
                if (remaining < 0) break;

                int r = (int)((max - code) >> (int)(32 - len));

                (byte[] slice, int flag) = dict[r];
                if (flag == 0)
                {
                    dict[r] = (null, 0);
                    slice = Decompress(slice);
                    dict[r] = (slice, 1);
                }
                output.AddRange(slice);
            }
            return output.ToArray();
        }
    }
}
