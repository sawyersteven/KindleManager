using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCss.Tests
{
    [TestClass]
    public class ParserBaseTests
    {
        class TestParser : ParserBase
        {
            protected override object DoParsePrivate()
            {
                Assert.Fail();
                return null;
            }

            public void SkipSpaceTests()
            {
                Skip();
                Assert.AreEqual(0, this.Errors.Count);
                Assert.IsTrue(End);
            }

            public void ExpectName(string name)
            {
                Assert.AreEqual(name, PickName());
                Assert.AreEqual(0, this.Errors.Count);
            }
            public void ExpectNumber(string number)
            {
                Assert.AreEqual(number, PickNumber());
                Assert.AreEqual(0, this.Errors.Count);
            }

            public void ExpectString(string str, bool withError = false)
            {
                Errors.Clear();
                Assert.AreEqual(str, PickString());
                if (withError)
                    Assert.AreNotEqual(0, this.Errors.Count);
                else
                    Assert.AreEqual(0, this.Errors.Count);
            }

            public void TestSkipUntil(char c, bool end = true)
            {
                SkipUntil(c);
                Assert.AreEqual(c, CurrentChar);
                Index++;
                Assert.AreEqual(End, end);
            }

            public void IndexPlus(int cnt)
            {
                var i = Index;
                Index += cnt;
                Assert.IsTrue(Index > i);
                Assert.AreNotEqual(Index, i + cnt); // comments supposted to have been skipped
            }

            public void ExpectValue(string val)
            {
                Assert.AreEqual(val, PickValue());
            }
        }

        [TestMethod]
        public void SkipSpaces()
        {
            var tp = new TestParser();
            tp.SetContext("  \t \n \r ", 0);
            tp.SkipSpaceTests();
        }

        [TestMethod]
        public void SkipComments()
        {
            var tp = new TestParser();
            tp.SetContext("/* hello world */ ", 0);
            tp.SkipSpaceTests();
        }

        [TestMethod]
        public void SkipCommentsAndSpaces()
        {
            var tp = new TestParser();
            tp.SetContext("  /* hello  *//* world */  /* hello world */ ", 0);
            tp.SkipSpaceTests();
        }

        [TestMethod]
        public void GetWord()
        {
            var tp = new TestParser();
            tp.SetContext("  hello -bingo _toto hou-hou u2 -x -100px");
            tp.ExpectName("hello");
            tp.ExpectName("-bingo");
            tp.ExpectName("_toto");
            tp.ExpectName("hou-hou");
            tp.ExpectName("u2");
            tp.ExpectName("-x");
            tp.ExpectName(null);

            tp.SetContext("hello]");
            tp.ExpectName("hello");
            tp.ExpectName(null);
        }

        [TestMethod]
        public void GetNumber()
        {
            var tp = new TestParser();
            tp.SetContext("  1 1.5/*test*/.5 0.123 1234 -.5 -12 . ", 0);
            tp.ExpectNumber("1");
            tp.ExpectNumber("1.5");
            tp.ExpectNumber("0.5");
            tp.ExpectNumber("0.123");
            tp.ExpectNumber("1234");
            tp.ExpectNumber("-0.5");
            tp.ExpectNumber("-12");
            tp.ExpectNumber(null);

            tp = new TestParser();
            tp.SetContext("-", 0);
            tp.ExpectNumber(null);

            tp = new TestParser();
            tp.SetContext("12.5.1", 0);
            tp.ExpectNumber("12.5");

            tp = new TestParser();
            tp.SetContext("-x", 0);
            tp.ExpectNumber(null);
            tp.ExpectName("-x");
        }

        [TestMethod]
        public void GetString()
        {
            var tp = new TestParser();
            tp.SetContext("  1 \"te'st\" 'te\"st' 'test\\'x' 'test\\\\\\'x' 'abc\r'def' ", 0);
            tp.ExpectString("1");
            tp.ExpectString("\"te'st\"");
            tp.ExpectString("'te\"st'");
            tp.ExpectString("'test\\'x'");
            tp.ExpectString("'test\\\\\\'x'");
            tp.ExpectString("'abc\r", true);
            tp.ExpectString("'def'");
            tp.ExpectString(null);
        }

        [TestMethod]
        public void SkipUntilTest()
        {
            var tp = new TestParser();
            tp.SetContext(" dsl /* ; */ ;");
            tp.TestSkipUntil(';');
        }


        [TestMethod]
        public void ImplicitCommentSkip()
        {
            var tp = new TestParser();
            tp.SetContext(";/*/hidden/*/first x/**/second");
            tp.IndexPlus(2);
            tp.ExpectName("first");
            tp.IndexPlus(2);
            tp.ExpectName("second");
        }

        [TestMethod]
        public void Values()
        {
            var tp = new TestParser();
            tp.SetContext(" 1.5 hello 3 #fff #612 2px /*comment*/1em 5% 'bouh'");
            tp.ExpectValue("1.5");
            tp.ExpectValue("hello");
            tp.ExpectValue("3");
            tp.ExpectValue("#fff");
            tp.ExpectValue("#612");
            tp.ExpectValue("2px");
            tp.ExpectValue("1em");
            tp.ExpectValue("5%");
            tp.ExpectValue("'bouh'");
        }
    }
}
