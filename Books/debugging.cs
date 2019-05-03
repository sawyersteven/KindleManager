using ExtensionMethods;
using System.IO;
using System;
using System.Collections.Generic;

namespace Books
{
    struct Epubs
    {
        public static string bornUnder = @"C:\Users\Steven\Downloads\Born Under an Assumed Name.epub";
        public static string endersGame = @"C:\Users\Steven\Downloads\endersgame.epub";
        public static string lastColony = @"C:\Users\Steven\Downloads\John Scalzi - [OLD MAN'S WAR 03] - Last Colony, The (v4.0) (EPUB).epub";
    }

    struct Mobis
    {
        public static string starshipTroopers = @"C:\Users\Steven\Downloads\Starship Troopers - Robert A Heinlein.mobi";
        public static string equalRites = @"C:\Users\Steven\Downloads\Equal Rites - Terry Pratchett.mobi";
        public static string ghostBrigades = @"C:\Users\Steven\Downloads\The Ghost Brigades - John Scalzi.mobi";
        public static string redProphet = @"C:\Users\Steven\Downloads\Red Prophet - Orson Scott Card.mobi";
        public static string created = @"C:\Users\Steven\Documents\My Kindle Content\my_first_ebo.mobi";
    }


    /// <summary>
    /// Misc function for debugging and experimenting without cluttering up useful classes
    /// </summary>
    static class Debug
    {
        static string path = Mobis.starshipTroopers;

        static public void Open()
        {
            path = @"C:\Users\Steven\Desktop\testheaders.bin";
            Formats.Mobi.Book book = new Formats.Mobi.Book(path);
            book.PrintHeaders();
        }


        static public void DumpRecords()
        {
            string outpath = @"C:\Users\Steven\Desktop\RecordDump";


            if (Directory.Exists(outpath))
            {
                Console.WriteLine("Output dir exists, deleting...");
                Directory.Delete(outpath, true);
            }
            Directory.CreateDirectory(outpath);


            Formats.Mobi.Book book = new Formats.Mobi.Book(path);

            using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                for (int i = 1; i < book.PDBHeader.records.Length; i++)
                {
                    int offs = (int)book.PDBHeader.records[i];
                    int recNum = i * 2;

                    int len;
                    if (i == book.PDBHeader.records.Length-1)
                    {
                        len = 4;
                    }
                    else
                    {
                        len = (int)book.PDBHeader.records[i + 1] - offs;
                    }

                    reader.BaseStream.Seek(offs, SeekOrigin.Begin);
                    byte[] record = reader.ReadBytes(len);


                    string fname = $"record{(i*2).ToString("D6")}";
                    File.WriteAllBytes(Path.Combine(outpath, fname), record);
                }
            }
        }


        static public void EpubToMobi()
        {
            Formats.Epub book = new Formats.Epub(Epubs.endersGame);
            ConvertToMobi(book);
        }

        static public void DumpTextHtml()
        {
            //Formats.Epub book = new Formats.Epub(path);
            Formats.Mobi.Book book = new Formats.Mobi.Book(path);
            string t = book.RawText();

            File.WriteAllText(@"C:\Users\Steven\Desktop\RedProphet.html", t);
        }

        static public void DumpImages()
        {
            Formats.Epub book = new Formats.Epub(path);
            byte[][] images = book.Images();

            for (int i = 0; i < images.Length; i++)
            {
                File.WriteAllBytes($@"C:\Users\Steven\Desktop\BOOKEXTRACT\{(i + 1).ToString("D5")}.jpg", images[i]);
            }
        }

        static public void ConvertToMobi(Formats.IBook book)
        {

            string outpath = @"C:\Users\Steven\Desktop\Converted.mobi";

            if (File.Exists(outpath))
            {
                File.Delete(outpath);
            }

            Converters.Converters.ToMobi(book, outpath);

            Formats.Mobi.Book mobi = new Formats.Mobi.Book(outpath);
            mobi.PrintHeaders();
            File.WriteAllText(@"C:\Users\Steven\Desktop\ConvertedTextDump.html", mobi.RawText());

        }

        static public void PrintHeaders()
        {
            Formats.Mobi.Book book;
            book = new Formats.Mobi.Book(path);
            book.PrintHeaders();
        }
    }
}
