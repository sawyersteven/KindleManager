using NCss.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCss.Tests
{
    [TestClass]
    public class PropertyParserTests
    {
        [TestMethod]
        public void Simple()
        {
            var parser = new PropertyParser();
            parser.SetContext(" background-color: red ");
            var prop  = parser.DoParse();
            Assert.IsTrue(parser.End);
            Assert.AreEqual(0, parser.Errors.Count);
            Assert.IsTrue(prop.IsValid);
            Assert.AreEqual("background-color", prop.Name);
            Assert.AreEqual("background-color:red;", prop.ToString());
        }

        [TestMethod]
        public void DataUrl()
        {
            // contains ';', that's the heck !
            var parser = new PropertyParser();
            parser.SetContext("background-image:url(data:image/png;base64,XXXX)");
            var prop = parser.DoParse();
            Assert.IsTrue(parser.End);
            Assert.AreEqual(0, parser.Errors.Count);
            Assert.IsTrue(prop.IsValid);
            Assert.AreEqual("background-image:url(data:image/png;base64,XXXX);", prop.ToString());
        }

        [TestMethod]
        public void NestedFunctions()
        {
            var parser = new PropertyParser();
            parser.SetContext("x:f1(f2(1),f3(2px))");
            var prop = parser.DoParse();
            Assert.IsTrue(parser.End);
            Assert.AreEqual(0, parser.Errors.Count);
            Assert.IsTrue(prop.IsValid);
            Assert.AreEqual("x:f1(f2(1),f3(2px));", prop.ToString());
        }

        [TestMethod]
        public void FontZero()
        {
            var parser = new PropertyParser();
            parser.SetContext("font:0/0 a");
            var prop = parser.DoParse();
            Assert.IsTrue(parser.End);
            Assert.AreEqual(0, parser.Errors.Count);
            Assert.IsTrue(prop.IsValid);
            Assert.AreEqual("font:0/0 a;", prop.ToString());
        }
    }
}
