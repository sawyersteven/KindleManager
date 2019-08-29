using Microsoft.VisualStudio.TestTools.UnitTesting;
using NCss.Parsers;

namespace NCss.Tests
{
    [TestClass]
    public class BlockParserTests
    {
        static Stylesheet Test(string input, string expected = null, int count = 1)
        {
            expected = expected ?? input;
            var parser = new StylesheetParser();
            parser.SetContext(input);
            var p = parser.DoParse();
            Assert.IsTrue(parser.End);
            Assert.AreEqual(0, parser.Errors.Count);
            Assert.IsTrue(p.IsValid, "invlid css");
            Assert.AreEqual(count, p.Rules.Count);
            Assert.AreEqual(expected, p.ToString());
            return p;
        }


        [TestMethod]
        public void _1_OneClassNoContent()
        {
            var sh = ".class{}";
            Test(sh, null);
            Test(".class {}", sh);
            Test(" .class { \n  } ", sh);
            Test(" .class[cond] test { \n  } ", ".class[cond] test{}");
        }

        [TestMethod]
        public void _2_MultipleClassesNoContent()
        {
            var sh = ".c1{}#c2{}";
            Test(sh, count: 2);
            Test(".c1{ \n }\n  #c2{ }  ", sh, count: 2);
        }

        [TestMethod]
        public void _3_SingleWithContent()
        {
            var sh = ".cl{background-color:red;margin:5px;}";
            Test(sh);
        }

    }
}
