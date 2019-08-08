using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils.Decompressors
{
    /// <summary>
    /// I'll be completely honest -- I have no idea what any of this does.
    /// I ported the algorithm from MobiUnpack and optimized it however I
    /// could without breaking it. There seems to be no  official docs
    /// for this, which should not be surprising for anything related
    /// to MOBIs.
    /// https://github.com/siebert/mobiunpack/blob/master/mobiunpack.py
    /// </summary>
    public class HuffCdic : IDecompressor
    {
        private static readonly byte[] HuffHeader = new byte[] { 0x48, 0x55, 0x46, 0x46, 0x00, 0x00, 0x00, 0x18 };
        private static readonly byte[] CdicHeader = new byte[] { 0x43, 0x44, 0x49, 0x43, 0x00, 0x00, 0x00, 0x10 };

        private List<uint> mincodes = new List<uint>() { 0 };
        private List<uint> maxcodes = new List<uint>() { uint.MaxValue };
        private List<(byte[], int)> dict = new List<(byte[], int)>();
        private (uint, uint, uint)[] dict1 = new (uint, uint, uint)[256];

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
            if (!huffRecord.SubArray(0, 0x8).SequenceEqual(HuffHeader))
            {
                throw new ArgumentException("HUFF record header invalid");
            }
            int offs1 = (int)BigEndian.ToUInt32(huffRecord, 0x8);
            int offs2 = (int)BigEndian.ToUInt32(huffRecord, 0xC);

            for (int i = 0; i < 256; i++)
            {
                dict1[i] = DictUnpack(BigEndian.ToUInt32(huffRecord, offs1 + (4 * i)));
            }

            for (int i = 0; i < 64; i += 2)
            {
                uint min = BigEndian.ToUInt32(huffRecord, offs2 + (4 * i));
                uint max = BigEndian.ToUInt32(huffRecord, offs2 + (4 * (i + 1)));

                mincodes.Add(min << (0x20 - ((i / 2) + 1)));
                int j = (i / 2) + 1;
                maxcodes.Add(((max + 1) << (0x20 - j)) - 1);
            }
        }

        private void ReadCdicRecord(byte[] cdicRecord)
        {
            if (!cdicRecord.SubArray(0, 0x8).SequenceEqual(CdicHeader))
            {
                throw new ArgumentException("CDIC record header invalid");
            }
            int phrases = (int)BigEndian.ToUInt32(cdicRecord, 0x8);
            int bits = (int)BigEndian.ToUInt32(cdicRecord, 0xc);

            int n = Math.Min(1 << bits, phrases - dict.Count);
            for (int i = 0; i < n; i++)
            {
                ushort offs = BigEndian.ToUInt16(cdicRecord, 0x10 + (i * 2));
                ushort blen = BigEndian.ToUInt16(cdicRecord, 0x10 + offs);
                var slice = cdicRecord.SubArray(0x12 + offs, blen & 0x7fff);
                dict.Add((slice, blen & 0x8000));
            }
        }

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

        public byte[] Decompress(byte[] data)
        {
            List<byte> output = new List<byte>();

            int remaining = data.Length * 8;

            data = data.Append(new byte[8]);
            int pos = 0;

            ulong x = BigEndian.ToUInt64(data, pos);
            int n = 32;

            while (true)
            {
                if (n <= 0)
                {
                    pos += 4;
                    x = BigEndian.ToUInt64(data, pos);
                    n += 32;
                }

                ulong code = (x >> n) & uint.MaxValue;

                (uint codelen, uint term, uint maxcode) = dict1[code >> 24];

                if (term == 0)
                {
                    while (code < mincodes[(int)codelen])
                    {
                        codelen += 1;
                    }
                    maxcode = maxcodes[(int)codelen];
                }

                n -= (int)codelen;
                remaining -= (int)codelen;
                if (remaining < 0) break;

                int r = (int)((maxcode - code) >> (int)(32 - codelen));
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
