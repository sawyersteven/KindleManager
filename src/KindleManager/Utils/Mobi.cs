using System;
using System.Collections.Generic;
using System.Globalization;

namespace Utils
{
    public static class Mobi
    {
        private static readonly CultureInfo culture = new CultureInfo("en-US");

        /// <summary>
        /// Parse forward-encoded Mobipocket variable-width int
        /// Retuns int with optional out param for number of bytes used to create int
        /// 
        /// https://wiki.mobileread.com/wiki/MOBI#Variable-width_integers
        /// </summary>
        /// <param name="buffer"> At least four bytes read from end of text record</param>
        /// <returns></returns>
        public static uint DecodeVWI(byte[] src, bool forward, out int i)
        {
            uint val = 0;
            if (!forward) Array.Reverse(src);

            List<byte> usable = new List<byte>();
            foreach (byte b in src)
            {
                usable.Add((byte)(b & 0x7F));
                if ((b & 0x80) != 0) break;
            }

            if (!forward) usable.Reverse();

            foreach (byte b in usable)
            {
                val <<= 7;
                val |= b;
            }
            i = usable.Count;
            return val;
        }

        public static uint DecodeVWI(byte[] buffer)
        {
            return DecodeVWI(buffer, true, out int _);
        }

        /// <summary>
        /// Turns uint into big-endian VarLengthInt byte array
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static byte[] EncodeVWI(uint value, bool forward)
        {
            List<byte> output = new List<byte>();

            while (true)
            {
                uint b = value & 0x7F;
                value >>= 7;
                output.Add((byte)b);
                if (value == 0) break;
            }

            if (forward)
            {
                output[0] |= 0x80;
                output.Reverse();
            }
            else
            {
                output.Reverse();
                output[0] |= 0x80;
            }

            return output.ToArray();
        }

        /// <summary>
        /// Counts number of bits set in byte
        /// EG: 01100110 => 4
        /// </summary>
        public static int CountBits(byte b)
        {
            int count = 0;
            while (b > 0)
            {
                if ((b & 0x01) == 0x01)
                {
                    count++;
                }
                b >>= 1;
            }
            return count;
        }

        /// <summary>
        /// Formats app standard date M/d/yyyy to mobi standard yyyy-MM-dd
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string FormatDate(string date)
        {
            return DateTime.ParseExact(date, "M/d/yyyy", culture).ToString("yyyy-MM-dd");
        }
    }
}
