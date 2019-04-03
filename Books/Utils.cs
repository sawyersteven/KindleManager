using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExtensionMethods;

namespace Utils
{
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
