using Microsoft.VisualStudio.TestTools.UnitTesting;
using NCss.Parsers;
using System.Linq;

namespace NCss.Tests
{
    [TestClass]
    public class HacksTests
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
        public void WithStar()
        {
            var sh = ".cl{*prop:white;}";
            var p = Test(sh);
            Assert.IsTrue(p.Rules.Single().Properties.Single().HasStar);
        }

        [TestMethod]
        public void With9()
        {

            var sh = "div{margin-top:1px\\9;}";
            var p = Test(sh);
            Assert.IsTrue(p.Rules.Single().Properties.Single().HasSlash9);
        }

        [TestMethod]
        public void With0()
        {
            var sh = "#div{height:300px\\0/;}";
            var p = Test(sh);
            Assert.IsTrue(p.Rules.Single().Properties.Single().HasSlash0);
        }

        [TestMethod]
        public void IE_WithDxTransform()
        {
            var sh = "div{filter:progid:DXImageTransform.Microsoft.gradient(enabled=false);}";
            Test(sh);
        }

        [TestMethod]
        public void IE6Only()
        {
            var sh = "* html #div{height:300px;}";
            Test(sh);
        }

        [TestMethod]
        [Ignore] // To be fixed ? that's a pretty shitty & useless one.
        public void Safari2Opera925()
        {
            // Parsed as "* #catorce{color:red;}"
            // ... but marked as invalid.
            // When fixed, uncomment corresponding line in hacks.css
            var sh = "*|html[xmlns*=\"\"] #catorce { color: red  }";
            Test(sh);
        }

        [TestMethod]
        public void IE7Only()
        {
            var sh = "*+html #div{height:300px;}";
            Test(sh);
        }

        [TestMethod]
        public void IE6Prop()
        {
            var sh = ".class{_prop:val;}";
            Test(sh);
        }

        [TestMethod]
        public void BangHack()
        {
            var sh = ".class{_prop:val!ie7;}";
            Test(sh);
        }

    }
}
