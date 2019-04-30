using ExtensionMethods;
using System;
using System.Collections.Generic;

namespace Formats.Mobi.Records
{
    public struct MetaINDX
    {
        public byte[] buffer;

        public string magic;
        public uint length;
        public uint type;
        public byte[] unused;

        public byte[] unused2;
        public uint idxtOffset;
        public uint recordCount;
        public uint recordEncoding;

        public uint languageCode;
        public uint recordEntryCount;
        public uint odrtOffset;
        public uint ligtOffset;

        public byte[] unused3;

        public string tagxMagic;
        public uint tagxLength;
        public uint tagxControlByteCount;
        public byte[][] tagxTable;

        /// <summary>
        /// First INDX record with TAGX table
        /// Holds information regarding DataINDX records
        /// </summary>
        /// <param name="buffer"></param>
        public MetaINDX(byte[] buffer)
        {
            Utils.BitConverter.LittleEndian = false;

            this.buffer = buffer;

            magic = buffer.SubArray(0, 0x4).Decode();
            length = Utils.BitConverter.ToUInt32(buffer, 0x4);
            type = Utils.BitConverter.ToUInt32(buffer, 0x8);
            unused = buffer.SubArray(0xC, 0x4);

            unused2 = buffer.SubArray(0x10, 0x4);
            idxtOffset = Utils.BitConverter.ToUInt32(buffer, 0x14);
            recordCount = Utils.BitConverter.ToUInt32(buffer, 0x18);
            recordEncoding = Utils.BitConverter.ToUInt32(buffer, 0x1c);

            languageCode = Utils.BitConverter.ToUInt32(buffer, 0x20);
            recordEntryCount = Utils.BitConverter.ToUInt32(buffer, 0x24);
            odrtOffset = Utils.BitConverter.ToUInt32(buffer, 0x28);
            ligtOffset = Utils.BitConverter.ToUInt32(buffer, 0x2c);

            unused3 = buffer.SubArray(0x30, (int)length - 0x30);

            int tagxOffset = (int)length;
            tagxMagic = buffer.SubArray((int)length, 0x4).Decode();
            if (tagxMagic == "TAGX")
            {
                tagxLength = Utils.BitConverter.ToUInt32(buffer, tagxOffset + 0x4);
                tagxControlByteCount = Utils.BitConverter.ToUInt32(buffer, tagxOffset + 0x8);
                tagxTable = new byte[(tagxLength - 12) / 4][];
                for (int i = 0; i < tagxTable.Length; i++)
                {
                    tagxTable[i] = buffer.SubArray(tagxOffset + 0xc + (i * 4), 0x4);
                }
            }
            else
            {
                tagxMagic = null;
                tagxLength = 0;
                tagxControlByteCount = 0;
                tagxTable = null;
            }
        }
    }

    public struct DataINDX
    {
        byte[] buffer;
        uint textEncoding;
        uint controlByteCount;
        byte[][] tagxTable;

        public string magic;
        public uint length;
        public uint type;
        public byte[] unused;

        public byte[] unused2;
        public uint idxtOffset;
        public uint recordEntryCount;

        /// <summary>
        /// INDX containing index table data
        /// </summary>
        public DataINDX(byte[] buffer, uint textEncoding, uint controlByteCount, byte[][] tagxTable)
        {
            Utils.BitConverter.LittleEndian = false;

            this.buffer = buffer;
            this.textEncoding = textEncoding;
            this.controlByteCount = controlByteCount;
            this.tagxTable = tagxTable;

            magic = buffer.SubArray(0, 0x4).Decode();
            length = Utils.BitConverter.ToUInt32(buffer, 0x4);
            type = Utils.BitConverter.ToUInt32(buffer, 0x8);
            unused = buffer.SubArray(0xC, 0x4);

            unused2 = buffer.SubArray(0x10, 0x4);
            idxtOffset = Utils.BitConverter.ToUInt32(buffer, 0x14);
            recordEntryCount = Utils.BitConverter.ToUInt32(buffer, 0x18);
        }


        public void ReadIDXT()
        {
            Utils.BitConverter.LittleEndian = false;

            if (buffer.SubArray((int)idxtOffset, 0x4).Decode() != "IDXT")
            {
                throw new Exception("Invalid IDXT magic");
            }

            ushort[] indxRecords = new ushort[recordEntryCount+1];

            for (int i = 0; i < recordEntryCount; i++)
            {
                indxRecords[i] = Utils.BitConverter.ToUInt16(buffer, (int)idxtOffset + 4 + (i * 2));
            }
            indxRecords[recordEntryCount] = (ushort)idxtOffset;

            // DEBUGGING: ok to here
            
            // each record entry has:
            //  [1] byte signifying string length
            //  [n] bytes of string encoded in textEncoding
            //  [remainder] bytes for tagx table
            for (int i = 0; i < recordEntryCount; i++)
            { 
                int reclen = indxRecords[i + 1] - indxRecords[i];
                byte[] rec = buffer.SubArray(indxRecords[i], reclen);

                byte strlen = rec[0];
                string recName;
                if (textEncoding == 65001)
                {
                    recName = rec.SubArray(0x1, strlen).Decode();
                }
                else
                {
                    recName = rec.SubArray(0x1, strlen).Decode("utf-16");
                }

                var tagMap = TagMap(rec.SubArray(strlen+1, rec.Length - (strlen+1)));
            }
        }

        /// <summary>
        /// Creates map of tags and values from an INDX record
        /// 
        /// tags consist of byte[tagid, valuesPerEntry, mask, endFlag]
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        private Dictionary<int, int[]> TagMap(byte[] record)
        {
            List<int[]> tags = new List<int[]>();
            Dictionary<int, int[]> tagMap = new Dictionary<int, int[]>();

            int controlByteIndex = 0;
            int consumedBytes = (int)controlByteCount;

            foreach (byte[] tag in tagxTable)
            {
                byte mask = tag[2];

                if (tag[3] == 0x01)
                {
                    controlByteIndex++;
                    continue;
                }

                int val = record[controlByteIndex] & mask;
                if (val != 0)
                {
                    if (val == mask)
                    {
                        if(Utils.Mobi.CountBits(mask) > 1)
                        {
                            val = Utils.Mobi.VarLengthInt(record.SubArray(consumedBytes, record.Length - consumedBytes), out int c);
                            consumedBytes += c;
                            tags.Add(new int[] { tag[0], tag[1], 0, val });
                        }
                        else
                        {
                            tags.Add(new int[] { tag[0], tag[1], 1, 0 });
                        }
                    }
                    else
                    {
                        while ((mask & 0x1) == 0)
                        {
                            mask >>= 1;
                            val >>= 1;
                        }
                        tags.Add(new int[] { tag[0], tag[1], val, 0 });
                    }
                }
            }

            // Processed tags are int[tagid, valuesPer, valueCount, valueBytes]
            List<int> values = new List<int>();
            foreach (int[] tag in tags)
            {
                values.Clear();

                if (tag[2] == 0) 
                {
                    int i = 0;
                    while (i < tag[3])
                    {
                        values.Add(Utils.Mobi.VarLengthInt(record.SubArray(consumedBytes, record.Length - consumedBytes), out int c));
                        consumedBytes += c;
                        i += c;
                    }
                    if (i != tag[3])
                    {
                        Console.WriteLine($"Tagx entry used {i} but should have used {tag[3]}");
                    }
                }
                else
                {
                    for (int i = 0; i < tag[1] * tag[2]; i++)
                    {
                        values.Add(Utils.Mobi.VarLengthInt(record.SubArray(consumedBytes, record.Length - consumedBytes), out int c));
                        consumedBytes += c;
                    }
                }

                tagMap.Add(tag[0], values.ToArray());
            }
            return tagMap;
        }
    }
}
