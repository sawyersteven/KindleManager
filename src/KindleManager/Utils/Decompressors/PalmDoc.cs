using ExtensionMethods;
using System.Collections.Generic;

namespace Utils.Decompressors
{
    public class PalmDoc : IDecompressor
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
            List<byte> output = new List<byte>();
            int pos = 0;
            buffer = TrimTrailingEntries(buffer);
            while (pos < buffer.Length)
            {
                byte b = buffer[pos];
                pos++;
                if (b >= 1 && b <= 8)
                {
                    output.AddRange(buffer.SubArray(pos, b));
                    pos += b;
                }
                else if (b < 128)
                {
                    output.Add(b);
                }
                else if (b >= 192)
                {
                    output.Add((byte)' ');
                    output.Add((byte)(b ^ 128));
                }
                else
                {
                    if (pos < buffer.Length)
                    {
                        int key = (b << 8) | buffer[pos];
                        pos++;

                        int start = (key >> 3) & 0x7FF;
                        int len = (key & 7) + 3;

                        if (start > len)
                        {
                            output.AddRange(output.GetRange(output.Count - start, len));
                        }
                        else
                        {
                            for (int i = 0; i < len; i++)
                            {
                                if (start == 1)
                                {
                                    output.Add(output[output.Count - 1]);
                                }
                                else
                                {
                                    output.Add(output[output.Count - start]);
                                }
                            }
                        }
                    }
                }

            }
            return output.ToArray();
        }

        /// <summary>
        /// Calculate total length of compressed data by subtracing
        ///     extra record bytes from end of text record
        /// </summary>
        /// Crawls backward through buffer to find length of all extra records
        private byte[] TrimTrailingEntries(byte[] record)
        {
            for (int _ = 0; _ < flagTrailingEntries; _++)
            {
                record = record.SubArray(0, record.Length - TrailingEntrySize(record));
            }
            if (flagMultiByte)
            {
                int len = (record[record.Length - 1] & 0x3) + 1;
                record = record.SubArray(0, record.Length - len);
            }
            return record;
        }

        private int TrailingEntrySize(byte[] buffer)
        {
            int s = 0;
            foreach (byte b in buffer.SubArray(buffer.Length - 4, 4))
            {
                if ((b & 0x80) != 0) s = 0;
                s = (s << 7) | (b & 0x7f);
            }
            return s;
        }
    }
}
