using System;
using System.Collections.Generic;
using ExtensionMethods;
using System.IO;
using HtmlAgilityPack;

namespace Formats
{

    public class MobiBuilder
    {
        private const int postHeaderPadding = 0x2000;

        private static byte[] nullTwo = new byte[2];
        private static byte[] nullFour = new byte[4];

        private static readonly byte[] FLISRecord = new byte[] { 0x46, 0x4C, 0x49, 0x53,
                                                          0x00, 0x00, 0x00, 0x08,
                                                          0x00, 0x41,
                                                          0x00, 0x00,
                                                          0x00, 0x00, 0x00, 0x00,
                                                          0xFF, 0xFF, 0xFF, 0xFF,
                                                          0x00, 0x01,
                                                          0x00, 0x03,
                                                          0x00, 0x00, 0x00, 0x03,
                                                          0x00, 0x00, 0x00, 0x01,
                                                          0xFF, 0xFF, 0xFF, 0xFF
                                                         };
        private static readonly byte[] EOFRecord = new byte[] { 0xe9, 0x8e, 0x0d, 0x0a };

        private static byte[] FCISRecord(uint textLength)
        {
            Utils.BitConverter.LittleEndian = false;

            byte[] rec = new byte[]{ 0x46, 0x43, 0x49, 0x53,
                                     0x00, 0x00, 0x00, 0x14,
                                     0x00, 0x00, 0x00, 0x10,
                                     0x00, 0x00, 0x00, 0x01,
                                     0x00, 0x00, 0x00, 0x00,
                                     0xFF, 0xFF, 0xFF, 0xFF, // this gets replaced by textLength
                                     0x00, 0x00, 0x00, 0x00,
                                     0x00, 0x00, 0x00, 0x20,
                                     0x00, 0x00, 0x00, 0x08,
                                     0x00, 0x01,
                                     0x00, 0x01,
                                     0x00, 0x00, 0x00, 0x00
                                    };

            byte[] tl = Utils.BitConverter.GetBytes(textLength);

            for (int i = 0; i < 4; i++)
            {
                rec[20 + i] = tl[i];
            }
 
            return rec;
        }

        public static void Convert(IBook donor, string outputPath)
        {
            if (donor == null)
            {
                throw new ArgumentException("Input book cannot be null");
            }

            string decodedText = FixImageRecIndexes(donor.TextContent());


            byte[] textBytes = decodedText.Encode();

            List<byte[]> textRecords = new List<byte[]>();
            
            for (int i = 0; i < textBytes.Length; i += 4096)
            {
                int len = Math.Min(4096, textBytes.Length - i);
                textRecords.Add(textBytes.SubArray(i, len));
            }

            byte[][] imageRecords = donor.Images();

            // Build headers backward to know lengths
            byte[] exthheader = EXTHHeader(donor);
            byte[] mobiheader = MobiHeader(donor, (uint)textRecords.Count, (uint)imageRecords.Length, (uint)exthheader.Length);
            byte[] palmdocheader = PalmDOCHeader((uint)decodedText.Length, (ushort)textRecords.Count);
            byte[] headersRecord = palmdocheader.Append(mobiheader.Append(exthheader));


            List<byte[]> dataRecords = new List<byte[]>();

            dataRecords.AddRange(textRecords);
            dataRecords.AddRange(imageRecords);

            dataRecords.Add(FLISRecord);
            dataRecords.Add(FCISRecord((uint)decodedText.Length));
            dataRecords.Add(EOFRecord);

            byte[] pdbheader = PDBHeader(donor, headersRecord, dataRecords.ToArray());

            using (FileStream file = new FileStream(outputPath, FileMode.CreateNew))
            using (BinaryWriter writer = new BinaryWriter(file))
            {
                writer.Write(pdbheader);
                writer.Write(headersRecord);
                writer.BaseStream.Seek(postHeaderPadding, SeekOrigin.Current);
                foreach (byte[] record in dataRecords)
                {
                    writer.Write(record);
                }
            }
        }

        private static byte[] PDBHeader(IBook donor, byte[] headersRecord, byte[][] dataRecords)
        {
            byte[] timestamp = BitConverter.GetBytes((uint)Utils.Metadata.TimeStamp());
            int pdbLen = 0x50 + (0x8 * dataRecords.Length);


            Utils.BitConverter.LittleEndian = false;


            List<byte> header = new List<byte>();
            string shortTitle = donor.Title.Length > 0x20 ? donor.Title.Substring(0x0, 0x20) : donor.Title + new byte[0x20 - donor.Title.Length].Decode();
            header.AddRange(shortTitle.Encode());                           // Title truncated to 32 bytes
            header.AddRange(nullTwo);                                       // Attributes
            header.AddRange(nullTwo);                                       // Version
            header.AddRange(timestamp);                                     // Created date
            header.AddRange(timestamp);                                     // Modified date
            header.AddRange(nullFour);                                      // Last backup
            header.AddRange(nullFour);                                      // Modification num
            header.AddRange(nullFour);                                      // App info id
            header.AddRange(nullFour);                                      // Sort info id
            header.AddRange("BOOK".Encode());                               // Type
            header.AddRange("MOBI".Encode());                               // Creator
            header.AddRange(Utils.BitConverter.GetBytes(Utils.Metadata.RandomNumber())); // Unique id seed
            header.AddRange(nullFour);                                      // Next record list id
            header.AddRange(Utils.BitConverter.GetBytes((ushort)(dataRecords.Length + 1)));   // Record count

            // Mobi header start                                            // Record offsets
            header.AddRange(Utils.BitConverter.GetBytes(pdbLen));
            header.AddRange(nullFour);
  
            int currentPosition = pdbLen + headersRecord.Length + postHeaderPadding;
            for (int i = 0; i < dataRecords.Length-1; i++)                 
            {
                header.AddRange(Utils.BitConverter.GetBytes(currentPosition));
                header.AddRange(Utils.BitConverter.GetBytes((i * 2) & 0x00FFFFFF));
                currentPosition += dataRecords[i].Length;
            }
                                                                           
            header.AddRange(nullTwo);                                       // 2 bytes of filler

            return header.ToArray();
        }

        private static byte[] PalmDOCHeader(uint textLength, ushort textRecordCount)
        {
            Utils.BitConverter.LittleEndian = false;
            List<byte> header = new List<byte>();

            header.AddRange(Utils.BitConverter.GetBytes((ushort)0x01)); // Compression (none)
            header.AddRange(nullTwo);                                   // Unused byte[2]
            header.AddRange(Utils.BitConverter.GetBytes(textLength));   // Length of text content string
            header.AddRange(Utils.BitConverter.GetBytes(textRecordCount));  // Number of text records
            header.AddRange(Utils.BitConverter.GetBytes((ushort)4096)); // Record size in bytes
            header.AddRange(Utils.BitConverter.GetBytes((ushort)0x0));  // Encryption type (none)
            header.AddRange(nullTwo);                                   // Unused byte[2]

            return header.ToArray();
        }

        private static byte[] MobiHeader(IBook donor, uint textRecordCount, uint imageRecordCount, uint exthLength)
        {
            Utils.BitConverter.LittleEndian = false;
            List<byte> header = new List<byte>();

            header.AddRange("MOBI".Encode());                                       // Mobi ID
            header.AddRange(Utils.BitConverter.GetBytes((uint)0xe8));               // Header length
            header.AddRange(Utils.BitConverter.GetBytes((uint)0x2));                // Mobi type (book)
            header.AddRange(Utils.BitConverter.GetBytes((uint)65001));              // Text encoding (utf8)
            header.AddRange(Utils.BitConverter.GetBytes((uint)Utils.Metadata.RandomNumber())); // UUID
            header.AddRange(Utils.BitConverter.GetBytes((uint)0x6));                // File version

            byte[] reserved = new byte[0x28];
            for (int i = 0; i < reserved.Length; i++){ reserved[i] = 0xFF; }
            header.AddRange(new byte[0x28]);                                        // 40 reserved bytes

            uint firstNonBook = imageRecordCount > 0 ? textRecordCount + imageRecordCount + 2 : 0;
            header.AddRange(Utils.BitConverter.GetBytes(firstNonBook));             // First non-book index

            header.AddRange(Utils.BitConverter.GetBytes(exthLength + 0xe8 +0x10));  // Full name offset (exth length + mobi length + paldoc length)

            header.AddRange(Utils.BitConverter.GetBytes((uint)donor.Title.Length)); // Full name length
            header.AddRange(Utils.BitConverter.GetBytes((uint)0x9));                // Locale
            header.AddRange(Utils.BitConverter.GetBytes((uint)0x6));                // Input language
            header.AddRange(Utils.BitConverter.GetBytes((uint)0x0));                // Output language
            header.AddRange(Utils.BitConverter.GetBytes((uint)0x6));                // Minimum version
            header.AddRange(Utils.BitConverter.GetBytes(textRecordCount + 1));      // First image index

            header.AddRange(Utils.BitConverter.GetBytes((uint)0x0));                // Huffman record count
            header.AddRange(nullFour);                                              // Huffman table offset
            header.AddRange(Utils.BitConverter.GetBytes((uint)0x0));                // Huffman record offset
            header.AddRange(nullFour);                                              // Huffman table length
            header.AddRange(Utils.BitConverter.GetBytes((uint)0x40));               // EXTH flags

            header.AddRange(new byte[0x20]);                                        // Unused 32 bytes
            header.AddRange(Utils.BitConverter.GetBytes(0xFFFFFFFF));               // DRM offset (non-extant)
            header.AddRange(Utils.BitConverter.GetBytes(0xFFFFFFFF));               // DRM count (non-extant)
            header.AddRange(Utils.BitConverter.GetBytes((uint)0x0));                // DRM flags
            header.AddRange(new byte[0x48]);                                        // 72 bytes of filler

            return header.ToArray();
        }

        /// <summary>
        /// https://wiki.mobileread.com/wiki/MOBI#EXTH_Header
        /// </summary>
        /// <returns></returns>
        private static byte[] EXTHHeader(IBook donor) {
            Utils.BitConverter.LittleEndian = false;

            uint addRecord(List<byte> records, uint type, string val)
            {
                if (val == "") return 0;
                records.AddRange(Utils.BitConverter.GetBytes(type));
                records.AddRange(Utils.BitConverter.GetBytes((uint)val.Length + 8));
                records.AddRange(val.Encode());
                return 1;
            }

            uint recordCount = 0;
            List<byte> exthrecords = new List<byte>();
            recordCount += addRecord(exthrecords, 100, donor.Author);
            recordCount += addRecord(exthrecords, 101, donor.Publisher);
            recordCount += addRecord(exthrecords, 103, donor.Description);
            recordCount += addRecord(exthrecords, 104, donor.ISBN.ToString());
            recordCount += addRecord(exthrecords, 105, string.Join(", ", donor.Subject));
            recordCount += addRecord(exthrecords, 106, donor.PubDate);
            recordCount += addRecord(exthrecords, 108, "Lignum");
            recordCount += addRecord(exthrecords, 109, donor.Rights);
            recordCount += addRecord(exthrecords, 204, "Lignum");
            recordCount += addRecord(exthrecords, 524, donor.Language);

            List<byte> header = new List<byte>();
            header.AddRange("EXTH".Encode());                                           // ID
            header.AddRange(Utils.BitConverter.GetBytes((uint)exthrecords.Count + 12)); // Header length
            header.AddRange(Utils.BitConverter.GetBytes((uint)recordCount));            // Record count
            header.AddRange(exthrecords.ToArray());                                     // Records
            header.AddRange(new byte[header.Count % 4]);                                // Pad to next 4-bytes

            return header.ToArray();
        }

        private static string FixImageRecIndexes(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            foreach (HtmlNode img in doc.DocumentNode.SelectNodes("//img"))
            {
                string src = img.Attributes["src"].Value;
                if (src != null)
                {
                    img.SetAttributeValue("recindex", src);
                }
            }
            return doc.DocumentNode.OuterHtml;
        }

    }
}
