using ExtensionMethods;
using System;

namespace Utils
{
    /// <summary>
    /// BitConverter-like methods that *always* use big-endidian bytes
    /// </summary>
    static class BigEndian
    {
        private static readonly bool reverseBytes = BitConverter.IsLittleEndian;

        #region From Bytes
        public static short ToInt16(byte[] buffer, int offset)
        {
            if (!reverseBytes) return BitConverter.ToInt16(buffer, offset);

            byte[] slice = buffer.SubArray(offset, 0x2);
            Array.Reverse(slice);
            return BitConverter.ToInt16(slice, 0x0);
        }

        public static ushort ToUInt16(byte[] buffer, int offset)
        {
            if (!reverseBytes) return BitConverter.ToUInt16(buffer, offset);

            byte[] slice = buffer.SubArray(offset, 0x2);
            Array.Reverse(slice);
            return BitConverter.ToUInt16(slice, 0x0);
        }

        public static uint ToUInt32(byte[] buffer, int offset)
        {
            if (!reverseBytes) return BitConverter.ToUInt32(buffer, offset);

            byte[] slice = buffer.SubArray(offset, 0x4);
            Array.Reverse(slice);
            return BitConverter.ToUInt32(slice, 0x0);
        }

        public static ulong ToUInt64(byte[] buffer, int offset)
        {
            if (!reverseBytes) return BitConverter.ToUInt64(buffer, offset);

            byte[] slice = buffer.SubArray(offset, 0x8);
            Array.Reverse(slice);
            return BitConverter.ToUInt64(slice, 0x0);
        }

        public static int ToInt32(byte[] buffer, int offset)
        {
            if (!reverseBytes) return BitConverter.ToInt32(buffer, offset);

            byte[] slice = buffer.SubArray(offset, 0x4);
            Array.Reverse(slice);
            return BitConverter.ToInt32(slice, 0x0);
        }
        #endregion

        #region GetBytes
        // Why can't GetBytes be generic....
        public static byte[] GetBytes(short val)
        {
            byte[] output = BitConverter.GetBytes(val);
            if (reverseBytes) Array.Reverse(output);
            return output;
        }

        public static byte[] GetBytes(ushort val)
        {
            byte[] output = BitConverter.GetBytes(val);
            if (reverseBytes) Array.Reverse(output);
            return output;
        }

        public static byte[] GetBytes(int val)
        {
            byte[] output = BitConverter.GetBytes(val);
            if (reverseBytes) Array.Reverse(output);
            return output;
        }

        public static byte[] GetBytes(uint val)
        {
            byte[] output = BitConverter.GetBytes(val);
            if (reverseBytes) Array.Reverse(output);
            return output;
        }

        public static byte[] GetBytes(ulong val)
        {
            byte[] output = BitConverter.GetBytes(val);
            if (reverseBytes) Array.Reverse(output);
            return output;
        }
        #endregion
    }
}
