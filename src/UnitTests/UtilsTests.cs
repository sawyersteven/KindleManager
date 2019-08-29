using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using Utils;


namespace UnitTests.UtilsTests
{
    [TestClass]
    public class Test_Base32Int
    {
        [TestMethod]
        public void Test_TryParse()
        {
            Base32Int.TryParse("00123", out int i);
            Assert.AreEqual(i, 1091);

            bool success = Base32Int.TryParse("", out int j);
            Assert.AreEqual(success, false);
            Assert.AreEqual(j, 0);
        }
    }

    [TestClass]
    public class Test_BigEndian
    {
        #region from bytes
        [TestMethod]
        public void Test_ToInt16()
        {
            short v = BigEndian.ToInt16(new byte[] { 0xFF, 0x8 }, 0);
            Assert.AreEqual(v, -248);
        }

        [TestMethod]
        public void Test_ToUInt16()
        {
            ushort v = BigEndian.ToUInt16(new byte[] { 0xFF, 0x08 }, 0);
            Assert.AreEqual(v, 65288);
        }

        [TestMethod]
        public void Test_ToInt32()
        {
            int v = BigEndian.ToInt32(new byte[] { 0xFF, 0xAA, 0xBB, 0xCC }, 0);
            Assert.AreEqual(v, -5588020);
        }

        [TestMethod]
        public void Test_ToUInt32()
        {
            uint v = BigEndian.ToUInt32(new byte[] { 0xFF, 0xAA, 0xBB, 0xCC }, 0);
            Assert.AreEqual(v, 4289379276);
        }

        [TestMethod]
        public void Test_ToUInt64()
        {
            ulong v = BigEndian.ToUInt64(new byte[] { 0xFF, 0xAA, 0xBB, 0xCC, 0xFF, 0xAA, 0xBB, 0xCC }, 0);
            Assert.AreEqual(v, 18422743714849536972);
        }
        #endregion

        #region get bytes
        [TestMethod]
        public void Test_GetBytesShort()
        {
            short t = -248;
            byte[] bytes = BigEndian.GetBytes(t);
            CollectionAssert.AreEqual(bytes, new byte[] { 0xFF, 0x8 });
        }

        [TestMethod]
        public void Test_GetBytesUShort()
        {
            ushort t = 65288;
            byte[] bytes = BigEndian.GetBytes(t);
            CollectionAssert.AreEqual(bytes, new byte[] { 0xFF, 0x08 });
        }

        [TestMethod]
        public void Test_GetBytesInt()
        {
            int t = -5588020;
            byte[] bytes = BigEndian.GetBytes(t);
            CollectionAssert.AreEqual(bytes, new byte[] { 0xFF, 0xAA, 0xBB, 0xCC });
        }

        [TestMethod]
        public void Test_GetBytesUInt()
        {
            uint t = 4289379276;
            byte[] bytes = BigEndian.GetBytes(t);
            CollectionAssert.AreEqual(bytes, new byte[] { 0xFF, 0xAA, 0xBB, 0xCC });
        }

        [TestMethod]
        public void Test_GetBytesULong()
        {
            ulong t = 18422743714849536972;
            byte[] bytes = BigEndian.GetBytes(t);
            CollectionAssert.AreEqual(bytes, new byte[] { 0xFF, 0xAA, 0xBB, 0xCC, 0xFF, 0xAA, 0xBB, 0xCC });
        }

        #endregion
    }

    [TestClass]
    public class Test_Files
    {
        [TestMethod]
        public void Test_MakeFileSystemSafe()
        {
            string validPath = Files.MakeFilesystemSafe("C:\\Users\\Admin\\Documents\\Library\\Author?Name::\\File<Name>.mobi");
            Assert.AreEqual(validPath, "C:\\Users\\Admin\\Documents\\Library\\Author_Name_\\File_Name_.mobi");
        }

        [TestMethod]
        public void Test_CleanBackward()
        {
            string rootDir = @"Testing\CleanBackwardTest";
            Directory.CreateDirectory(Path.Combine(rootDir, "DeleteMe"));
            Directory.CreateDirectory(Path.Combine(rootDir, "DeleteMe", "DeleteMe2"));
            Directory.CreateDirectory(Path.Combine(rootDir, "KeepMe"));
            File.Create(Path.Combine(rootDir, "KeepMe", "test.txt")).Close();

            Files.CleanBackward(Path.Combine(rootDir, "DeleteMe", "DeleteMe2"), rootDir);

            string[] children = Directory.GetFileSystemEntries(rootDir);

            Directory.Delete(rootDir, true);
            CollectionAssert.AreEqual(children, new string[] { Path.Combine(rootDir, "KeepMe") });
        }

        [TestMethod]
        public void Test_CleanForward()
        {
            string rootDir = @"Testing\CleanForwardTest";
            Directory.CreateDirectory(Path.Combine(rootDir, "DeleteMe"));
            Directory.CreateDirectory(Path.Combine(rootDir, "DeleteMe", "DeleteMe2"));
            Directory.CreateDirectory(Path.Combine(rootDir, "KeepMe"));
            File.Create(Path.Combine(rootDir, "KeepMe", "test.bin")).Close();

            Files.CleanForward(rootDir);

            string[] children = Directory.GetFileSystemEntries(rootDir);

            Directory.Delete(rootDir, true);
            CollectionAssert.AreEqual(children, new string[] { Path.Combine(rootDir, "KeepMe") });
        }
    }

    [TestClass]
    public class Test_Metadata
    {
        [TestMethod]
        public void Test_RandomNumber()
        {
            int rnd = Metadata.RandomNumber();
            Assert.IsTrue(rnd < 1000 && rnd > 99);
        }

        [TestMethod]
        public void Test_RandomNumber7Digits()
        {
            int rnd = Metadata.RandomNumber(7);
            Assert.IsTrue(rnd < 10000000 && rnd > 999999);
        }

        [TestMethod]
        public void Test_TimeStamp()
        {
            TimeSpan now = DateTime.UtcNow - new DateTime(1904, 1, 1);
            uint ts = Metadata.TimeStamp();
            Assert.AreEqual((uint)now.TotalSeconds / 10, ts / 10);
        }

        [TestMethod]
        public void Test_TimeStampFromDate()
        {
            TimeSpan target = new DateTime(1999, 12, 31) - new DateTime(1904, 1, 1);
            uint ts = Metadata.TimeStamp(1999, 12, 31);
            Assert.AreEqual(ts, (uint)target.TotalSeconds);
        }

        [TestMethod]
        public void Test_SortAuthor()
        {
            string chuck = Metadata.SortAuthor("Charles Dickens");
            string ursula = Metadata.SortAuthor("Ursula K. LeGuin");
            Assert.AreEqual(chuck, "Dickens, Charles");
            Assert.AreEqual(ursula, "LeGuin, Ursula K.");
        }

        [TestMethod]
        public void Test_GetDate()
        {
            Assert.AreEqual(Metadata.GetDate(""), DateTime.UtcNow.ToString("MM/dd/yyyy"));
            Assert.AreEqual(Metadata.GetDate("1999-12-31"), "12/31/1999");

        }
    }

    [TestClass]
    public class Test_Mobi
    {
        [TestMethod]
        public void Test_DecodeVWI_Reverse()
        {
            uint vli = Mobi.DecodeVWI(new byte[] { 0x99, 0x84, 0x22, 0x11 }, false, out int i);
            Assert.AreEqual((uint)69905, vli);
            Assert.AreEqual(3, i);
        }

        [TestMethod]
        public void Test_DecodeVWI_Forward()
        {
            uint vli = Mobi.DecodeVWI(new byte[] { 0x99, 0x84, 0x22, 0x11 }, true, out int i);
            Assert.AreEqual((uint)25, vli);
            Assert.AreEqual(1, i);
        }

        [TestMethod]
        public void Test_EncodeVWI_Reverse()
        {
            byte[] vli = Mobi.EncodeVWI(69905, false);
            CollectionAssert.AreEqual(vli, new byte[] { 0x84, 0x22, 0x11 });
        }

        [TestMethod]
        public void Test_EncodeVWI_Forward()
        {
            byte[] vli = Mobi.EncodeVWI(69905, true);
            CollectionAssert.AreEqual(vli, new byte[] { 0x4, 0x22, 0x91 });
        }

        [TestMethod]
        public void Test_CountBits()
        {
            int bits = Mobi.CountBits((byte)102);
            Assert.AreEqual(bits, 4);
        }

        [TestMethod]
        public void Test_FormatDate()
        {
            Assert.AreEqual(Mobi.FormatDate("12/31/1999"), "1999-12-31");
        }
    }

    [TestClass]
    public class Test_Decompressors
    {
        [TestMethod]
        public void Test_HuffCDIC()
        {
            Assert.Fail("I don't have an example of huffcdic compression handy.  But this doesn't matter so much since Mobi.Book.TextContent() is never called anyway.");
            //var decompressor = new Utils.Decompressors.HuffCdic();
        }

        [TestMethod] // Code coverage ftw
        public void Test_None()
        {
            byte[] compressed = new byte[] { 0x01, 0x02, 0x03 };
            var decompressor = new Utils.Decompressors.None();

            CollectionAssert.AreEqual(compressed, decompressor.Decompress(compressed));
        }

        [TestMethod]
        public void Test_PalmDoc_NoTrailing_NoMultibyte()
        {
            Assert.Fail("I don't have an example of PalmDoc compression handy. But this doesn't matter so much since Mobi.Book.TextContent() is never called anyway.");
            byte[] compressed = new byte[] { 0x01, 0x02, 0x03 };
            var decompressor = new Utils.Decompressors.PalmDoc(0, false);

            CollectionAssert.AreEqual(compressed, decompressor.Decompress(compressed));
        }

    }
}
