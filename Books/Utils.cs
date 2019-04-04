using System;
using ExtensionMethods;

namespace Utils
{
    class Metadata
    {
        /// <summary>
        /// Reorders author name for standard lastname-first sorting ie "Charles Dickens" becomes "Dickens, Charles"
        /// </summary>
        public static string SortAuthor(string author)
        {
            string[] splt = author.Split(' ');
            if (splt.Length == 1) return author;

            return splt[splt.Length - 1] + ", " + string.Join(" ", splt.SubArray(0, splt.Length - 1));
        }
    }

    class BitConverter
    {
        private static bool SwapEndian = false;
        private static bool _LittleEndian = System.BitConverter.IsLittleEndian;
        public static bool LittleEndian {
            get => _LittleEndian;
            set
            {
                _LittleEndian = value;
                if (value != System.BitConverter.IsLittleEndian)
                {
                    SwapEndian = true;
                }
            }
        }

        #region From Bytes
        public static short ToInt16(byte[] buffer, int offset)
        {
            if (!SwapEndian) return System.BitConverter.ToInt16(buffer, offset);

            byte[] slice = buffer.SubArray(offset, 0x2);
            Array.Reverse(slice);
            return System.BitConverter.ToInt16(slice, 0x0);
        }

        public static ushort ToUInt16(byte[] buffer, int offset)
        {
            if (!SwapEndian) return System.BitConverter.ToUInt16(buffer, offset);

            byte[] slice = buffer.SubArray(offset, 0x2);
            Array.Reverse(slice);
            return System.BitConverter.ToUInt16(slice, 0x0);
        }

        public static uint ToUInt32(byte[] buffer, int offset)
        {
            if (!SwapEndian) return System.BitConverter.ToUInt32(buffer, offset);

            byte[] slice = buffer.SubArray(offset, 0x4);
            Array.Reverse(slice);
            return System.BitConverter.ToUInt32(slice, 0x0);
        }
        #endregion

        #region GetBytes
        //Why can't GetBytes be generic....
        public static byte[] GetBytes(short val)
        {
            byte[] output = System.BitConverter.GetBytes(val);
            if (SwapEndian) { Array.Reverse(output); }
            return output;
        }

        public static byte[] GetBytes(ushort val)
        {
            byte[] output = System.BitConverter.GetBytes(val);
            if (SwapEndian) { Array.Reverse(output); }
            return output;
        }

        public static byte[] GetBytes(int val)
        {
            byte[] output = System.BitConverter.GetBytes(val);
            if (SwapEndian) { Array.Reverse(output); }
            return output;
        }

        public static byte[] GetBytes(uint val)
        {
            byte[] output = System.BitConverter.GetBytes(val);
            if (SwapEndian) { Array.Reverse(output); }
            return output;
        }
        #endregion
    }
}
