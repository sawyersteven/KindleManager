using ExtensionMethods;
using System;

namespace Utils.Decompressors
{
    class PalmDoc : IDecompressor
    {
        private readonly uint flagTrailingEntries;
        private readonly bool flagMultiByte;

        public PalmDoc(uint flagTrailingEntries, bool flagMultiByte)
        {
            this.flagTrailingEntries = flagTrailingEntries;
            this.flagMultiByte = flagMultiByte;
        }



        /// <summary>
        /// Decompress PalmDoc Compressed byte array
        /// </summary>
        public byte[] Decompress(byte[] buffer)
        {
            int compressedLen = CalcCompressedLen(buffer);

            byte[] output = new byte[DecompressedLength(buffer, compressedLen)];
            int i = 0;
            int j = 0;
            while (i < compressedLen)
            {
                int c = buffer[i++];

                if (c >= 0xc0)
                {
                    output[j++] = (byte)' ';
                    output[j++] = (byte)(c & 0x7f);
                }
                else if (c >= 0x80)
                {
                    c = (c << 8) + buffer[i++];
                    int windowLen = (c & 0x0007) + 3;
                    int windowDist = (c >> 3) & 0x07FF;
                    int windowCopyFrom = j - windowDist;

                    windowLen = Math.Min(windowLen, output.Length - j);

                    while (windowLen-- > 0)
                    {
                        output[j++] = output[windowCopyFrom++];
                    }
                }
                else if (c >= 0x09)
                {
                    output[j++] = (byte)c;
                }
                else if (c >= 0x01)
                {
                    c = Math.Min(c, output.Length - j);
                    while (c-- > 0)
                    {
                        output[j++] = buffer[i++];
                    }
                }
                else
                {
                    output[j++] = (byte)c;
                }
            }
            return output;
        }

        /// <summary>
        /// Calculate total length of compressed data by subtracing
        ///     extra record bytes from end of text record
        /// </summary>
        /// Crawls backward through buffer to find length of all extra records
        private int CalcCompressedLen(byte[] record)
        {
            int pos = record.Length;
            for (int _ = 0; _ < flagTrailingEntries; _++)
            {
                pos -= Mobi.VarLengthInt(record.SubArray(pos - 4, 0x4));
            }
            if (flagMultiByte)
            {
                pos -= (record[pos] & 0x3) + 1;
            }
            return pos;
        }

        /// <summary>
        /// Gets length of byte array after decompression
        /// </summary>
        private static int DecompressedLength(byte[] buffer, int compressedLen)
        {
            int i = 0;
            int len = 0;

            while (i < compressedLen)
            {
                int c = buffer[i++] & 0x00ff;
                if (c >= 0x00c0)
                {
                    len += 2;
                }
                else if (c >= 0x0080)
                {
                    c = (c << 8) | (buffer[i++] & 0x00FF);
                    len += 3 + (c & 0x0007);
                }
                else if (c >= 0x0009)
                {
                    len++;
                }
                else if (c >= 0x0001)
                {
                    len += c;
                    i += c;
                }
                else
                {
                    len++;
                }
            }
            return len;
        }
    }
}
