using ExtensionMethods;
using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace UnitTests.ExtensionMethodTests
{
    [TestClass]
    public class Test_HtmlNodeExtensions
    {
        private static readonly string htmlDoc = @"
        <html>
            <body>
                <div>
                    <p id='testDiv'></p>
                </div>
            </body>
        </html>
        ";

        [TestMethod]
        public void Test_BytePosition()
        {
            // This fails due to a dependency error I can't figure out atm.
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlDoc);
            HtmlNode testDiv = doc.GetElementbyId("testDiv");
            Assert.AreEqual(testDiv.BytePosition(), 0);
        }
    }

    [TestClass]
    public class Test_BinaryReaderExtensions
    {
        [TestMethod]
        public void Test_ReadAllBytes()
        {
            byte[] buff;
            using (BinaryReader reader = new BinaryReader(new FileStream("TESTREAD.bin", FileMode.Open)))
            {
                buff = reader.ReadAllBytes();
            }
            Assert.AreEqual(buff.Length, 4096);
        }
    }

    [TestClass]
    public class Test_ArrayExtensions
    {
        private static readonly int[] arr = new int[] { 1, 2, 3, 4, 5, 6 };

        [TestMethod]
        public void Test_Last()
        {
            Assert.AreEqual(arr.Last(), 6);
        }

        [TestMethod]
        public void Test_InsertRange()
        {
            int[] arrcpy = arr.InsertRange(2, new int[] { 9, 9, 9 });
            CollectionAssert.AreEqual(arrcpy, new int[] { 1, 2, 3, 9, 9, 9, 4, 5, 6 });
        }

        [TestMethod]
        public void Test_Append()
        {
            int[] arrcpy = arr.Append(new int[] { 9, 9, 9 });
            CollectionAssert.AreEqual(arrcpy, new int[] { 1, 2, 3, 4, 5, 6, 9, 9, 9, });
        }

        [TestMethod]
        public void Test_AppendItem()
        {
            int[] arrcpy = arr.Append(9);
            CollectionAssert.AreEqual(arrcpy, new int[] { 1, 2, 3, 4, 5, 6, 9 });
        }

        [TestMethod]
        public void Test_SubArray()
        {
            CollectionAssert.AreEqual(arr.SubArray(2, 2), new int[] { 3, 4 });
        }

        [TestMethod]
        public void Test_ReplaceAt()
        {
            int[] arrcpy = new int[arr.Length];
            arr.CopyTo(arrcpy, 0);
            arrcpy.ReplaceAt(new int[] { 9, 9 }, 2);
            CollectionAssert.AreEqual(arrcpy, new int[] { 1, 2, 9, 9, 5, 6 });
        }

        [TestMethod]
        public void Test_DecodeByteArray()
        {
            byte[] ba = new byte[] { 65, 66, 67, 68 };
            Assert.AreEqual("ABCD", ba.Decode());
        }

        [TestMethod]
        public void Test_DecodeByteArrayCP1252()
        {
            byte[] ba = new byte[] { 65, 66, 67, 68 };
            Assert.AreEqual("ABCD", ba.Decode("Windows-1252"));
        }
    }

    [TestClass]
    public class Test_ListExtensions
    {
        [TestMethod]
        public void Test_TotalLength()
        {
            List<int[]> lst = new List<int[]>();
            lst.Add(new int[] { 1, 2, 3, 4, 5 });
            lst.Add(new int[] { 1, 2, 3 });
            Assert.AreEqual(lst.TotalLength(), 8);
        }
    }

    [TestClass]
    public class Test_StringExtensions
    {
        private readonly string str = "Hello World!";

        [TestMethod]
        public void Test_Encode()
        {
            CollectionAssert.AreEqual(str.Encode(), new byte[] { 72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100, 33 });
        }

        [TestMethod]
        public void Test_Tuncate()
        {
            Assert.AreEqual(str.Truncate(5), "Hello");
            Assert.AreEqual(str.Truncate(12), str);
        }

        [TestMethod]
        public void Test_DictFormat()
        {
            Dictionary<string, string> formatter = new Dictionary<string, string>() {
                {"name", "Rick Sanchez" },
                {"universe", "C-137" }
            };

            string templ = "I'm {name} from universe {universe}, but I can't format {this}";
            Assert.AreEqual(templ.DictFormat(formatter), "I'm Rick Sanchez from universe C-137, but I can't format {this}");
        }

    }

    [TestClass]
    public class Test_DictExtensions
    {
        private readonly Dictionary<string, int?> dict = new Dictionary<string, int?>() {
            { "one", 1 },
            {"two", 2 },
            {"three", 3 }
        };

        [TestMethod]
        public void Test_Get()
        {
            Assert.AreEqual(dict.Get("one"), 1);
            Assert.AreEqual(dict.Get("four"), null);
        }
    }

    [TestClass]
    public class Test_HashSetExtensions
    {
        private readonly HashSet<int> hs = new HashSet<int>() { 1, 2, 3, 4, 5, 5, 5, 5, 5 };

        [TestMethod]
        public void Test_ToArray()
        {
            CollectionAssert.AreEqual(hs.ToArray(), new int[] { 1, 2, 3, 4, 5 });
        }
    }

    [TestClass]
    public class Test_ByteExtensions
    {
        private readonly byte A = 65;

        [TestMethod]
        public void Test_Decode()
        {
            Assert.AreEqual("A", A.Decode());
        }
    }
}
