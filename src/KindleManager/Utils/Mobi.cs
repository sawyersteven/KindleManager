using System;
using System.Collections.Generic;
using System.Globalization;

namespace Utils
{
    public static class Mobi
    {
        private static readonly CultureInfo culture = new CultureInfo("en-US");

        /// <summary>
        /// Parse backward-encoded Mobipocket variable-width int
        /// Retuns int with optional out param for number of bytes used to create int
        /// 
        /// https://wiki.mobileread.com/wiki/MOBI#Variable-width_integers
        /// </summary>
        /// <param name="buffer"> At least four bytes read from end of text record</param>
        /// <returns></returns>
        public static int VarLengthInt(byte[] buffer, out int c)
        {
            int varint = 0;
            c = 0;
            byte b;
            for (int i = 0; i < 4; i++)
            {
                b = buffer[i];
                c++;
                varint = (varint << 7) | (b & 0x7f);
                if ((b & 0x80) > 0)
                {
                    break;
                }
            }
            return varint;
        }

        public static int VarLengthInt(byte[] buffer)
        {
            return VarLengthInt(buffer, out int _);
        }

        /// <summary>
        /// Turns uint into big-endian VarLengthInt byte array
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static byte[] EncVarLengthInt(uint val)
        {
            List<byte> output = new List<byte>();

            while (output.Count < 4)
            {
                output.Add((byte)(val & 0x7f));
                val >>= 7;
                if (val == 0) break;
            }
            output[0] |= 0x80;
            output.Reverse();
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
