using System;
using System.Collections.Generic;
using ExtensionMethods;
using System.IO;
using HtmlAgilityPack;
using System.Linq;


// TODO: this works well enough but is a mess
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

            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(donor.TextContent());

            FixImageRecIndexes(html);
            //StripStyle(html);
            (string, int)[] tocData = FixLinks(html, true);

            string decodedText = html.DocumentNode.OuterHtml;

            byte[] textBytes = decodedText.Encode();

            List<byte[]> textRecords = new List<byte[]>();
            
            for (int i = 0; i < textBytes.Length; i += 4096)
            {
                int len = Math.Min(4096, textBytes.Length - i);
                textRecords.Add(textBytes.SubArray(i, len));
            }

            byte[][] imageRecords = donor.Images();

            byte[][] indxRecords = buildIndxRecords(tocData);

            // Build headers backward to know lengths
            byte[] exthheader = EXTHHeader(donor);
            byte[] mobiheader = MobiHeader(donor, (uint)textRecords.Count, (uint)imageRecords.Length, (uint)indxRecords.Length, (uint)exthheader.Length);
            byte[] palmdocheader = PalmDOCHeader((uint)textBytes.Length, (ushort)textRecords.Count);
            byte[] headersRecord = palmdocheader.Append(mobiheader.Append(exthheader));


            List<byte[]> dataRecords = new List<byte[]>();

            dataRecords.AddRange(textRecords);
            dataRecords.AddRange(indxRecords);
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
                writer.Write(donor.Title.Encode());
                writer.BaseStream.Seek(postHeaderPadding - donor.Title.Length, SeekOrigin.Current);
                foreach (byte[] record in dataRecords)
                {
                    writer.Write(record);
                }
            }
        }


        /// <summary>
        /// Makes INDX records
        /// https://wiki.mobileread.com/wiki/MOBI#Index_meta_record
        /// 
        /// Makes three records:
        ///     1: INDX record with TAGX
        ///     2: INDX record with logical toc numbers
        ///     3: Encoded text records with logical toc names
        /// </summary>
        /// <param name="toc">(string, int) of toc's (name, filepos)</param>
        /// <returns></returns>
        private static byte[][] buildIndxRecords((string, int)[] toc)
        {
            Utils.BitConverter.LittleEndian = false;

            List<byte[]> records = new List<byte[]>();

            List<byte> rec = new List<byte>();

            rec.AddRange("INDX".Encode());
            rec.AddRange(Utils.BitConverter.GetBytes((uint)192));
            rec.AddRange(Utils.BitConverter.GetBytes((uint)1));
            rec.AddRange(nullFour);

            rec.AddRange(nullFour);
            rec.AddRange(Utils.BitConverter.GetBytes((uint)232)); // change, idxt offset
            rec.AddRange(Utils.BitConverter.GetBytes((uint)1));
            rec.AddRange(Utils.BitConverter.GetBytes((uint)65001));

            rec.AddRange(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
            rec.AddRange(Utils.BitConverter.GetBytes(toc.Length));
            rec.AddRange(nullFour);
            rec.AddRange(nullFour);

            rec.AddRange(new byte[192 - rec.Count]);

            rec.AddRange("TAGX".Encode());


            return records.ToArray();
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
            header.AddRange(Utils.BitConverter.GetBytes((ushort)(dataRecords.Length)));   // Record count

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

        private static byte[] MobiHeader(IBook donor, uint textRecordCount, uint indxRecordCount, uint imageRecordCount, uint exthLength)
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
            header.AddRange(Utils.BitConverter.GetBytes(textRecordCount + indxRecordCount + 1));      // First image index

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
                if (val == "" || val == null) return 0;
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

        /// <summary>
        /// Changes img src to recindex
        /// </summary>
        private static void FixImageRecIndexes(HtmlDocument html)
        {
            HtmlNodeCollection imgs = html.DocumentNode.SelectNodes("//img");
            if (imgs == null) return;
            foreach (HtmlNode img in imgs)
            {
                string src = img.Attributes["src"].Value;
                if (src != null)
                {
                    img.SetAttributeValue("recindex", src);
                }
            }
        }

        /// <summary>
        /// Changes a href to filepos and adds TOC to end of document
        /// </summary>
        private static (string, int)[] FixLinks(HtmlDocument html, bool addTOC)
        {
            HtmlNode tocReference;

            if (addTOC && html.DocumentNode.SelectSingleNode("//html/head/guide/reference[@type='toc']") == null)
            {
                HtmlNode guide = HtmlNode.CreateNode("<guide></guide>");
                tocReference = HtmlNode.CreateNode("<reference title='Table of Contents' type='toc' filepos='0000000000'/>");
                HtmlNode head = html.DocumentNode.SelectSingleNode("//html/head");
                if (head == null)
                {
                    throw new Exception("Unable to find <head> element in html");
                }

                guide.ChildNodes.Append(tocReference);
                head.ChildNodes.Prepend(guide);
            };

            // Give all anchors filepos property then reload html to get correct streampositions
            HtmlNodeCollection anchors = html.DocumentNode.SelectNodes("//a");
            if (anchors == null) return new (string, int)[0];
            foreach (HtmlNode a in anchors)
            {
                if (a.Attributes["href"] != null)
                {
                    a.SetAttributeValue("filepos", 0.ToString("D10"));
                }
            }
            html.LoadHtml(html.DocumentNode.OuterHtml);

            // Get all anchors again and match them to a targetnode
            anchors = html.DocumentNode.SelectNodes("//a");
            if (anchors == null) return new (string, int)[0];
            List<(string, HtmlNode)> targetNodes = new List<(string, HtmlNode)>();
            foreach (HtmlNode a in anchors)
            {
                HtmlAttribute href = a.Attributes["href"];
                if (href == null) continue;
                HtmlNode target = html.DocumentNode.SelectSingleNode($"//*[@id='{href.Value.Substring(1)}']");
                if (target == null) continue;
                if (!targetNodes.Any(x => x.Item2 == target))
                {
                    targetNodes.Add((a.InnerText, target));
                }
                a.SetAttributeValue("filepos", target.BytePosition().ToString("D10"));
            }

            
            if (addTOC)
            {
                html.DocumentNode.SelectSingleNode("//html/body").InnerHtml += BuildTOC(targetNodes);
                html.LoadHtml(html.DocumentNode.OuterHtml);
                tocReference = html.DocumentNode.SelectSingleNode("//html/head/guide/reference[@type='toc']");
                tocReference.SetAttributeValue("filepos", html.DocumentNode.SelectSingleNode("//p[@id='toc']").BytePosition().ToString("D10"));
            }


            List<(string, int)> idxtTags = new List<(string, int)>();
            foreach ((string, HtmlNode) target in targetNodes)
            {
                idxtTags.Add((target.Item1, target.Item2.BytePosition()));
            }
            return idxtTags.ToArray();

        }

        private static string BuildTOC(List<(string, HtmlNode)> targets)
        {
            string[] parts = new string[targets.Count + 2];
            parts[0] = "<mbp:pagebreak/><p id='toc'>Table of Contents</p>";
            parts[parts.Length - 1] = "<mbp:pagebreak/>";

            string linkTemplate = "<blockquote><a filepos='{0}'>{1}</a></blockquote>";

            for (int i = 0; i < targets.Count; i++)
            {
                parts[i + 1] = string.Format(linkTemplate, targets[i].Item2.BytePosition().ToString("D10"), targets[i].Item1);
            }

            return string.Join("", parts);
        }

        private static void StripStyle(HtmlDocument html)
        {
            HtmlNode style = html.DocumentNode.SelectSingleNode("//html/head/style");
            if (style == null) return;
            style.Remove();

        }
    }
}
