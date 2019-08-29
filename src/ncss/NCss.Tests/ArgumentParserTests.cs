using Microsoft.VisualStudio.TestTools.UnitTesting;
using NCss.Parsers;

namespace NCss.Tests
{
    [TestClass]
    public class ArgumentParserTests
    {

        [TestMethod]
        public void PropertyValueWithFunction()
        {
            var parser = new CssSimpleValueParser();
            parser.SetContext("fn(1,2)");
            var arg = parser.DoParse();
            Assert.IsTrue(parser.End);
            Assert.AreEqual(0, parser.Errors.Count);
            Assert.IsTrue(arg.IsValid);
            Assert.AreEqual("fn(1,2)", arg.ToString());
        }

        [TestMethod]
        public void Simple()
        {
            var parser = new CssValueParser();
            parser.SetContext("#fff");
            var arg = parser.DoParse();
            Assert.IsTrue(parser.End);
            Assert.AreEqual(0, parser.Errors.Count);
            Assert.IsTrue(arg.IsValid);
            Assert.IsInstanceOfType(arg, typeof(CssSimpleValue));
            Assert.AreEqual("#fff", arg.ToString());
        }

        [TestMethod]
        public void SubArg()
        {
            var parser = new CssValueParser();
            parser.SetContext("fn(#fff)");
            var arg = parser.DoParse();
            Assert.IsTrue(parser.End);
            Assert.AreEqual(0, parser.Errors.Count);
            Assert.IsTrue(arg.IsValid);
            Assert.IsInstanceOfType(arg, typeof(CssSimpleValue));
            Assert.AreEqual("fn(#fff)", arg.ToString());
        }

        [TestMethod]
        public void Addition()
        {
            var parser = new CssValueParser();
            parser.SetContext("30%+10%");
            var arg = parser.DoParse();
            Assert.IsTrue(parser.End);
            Assert.AreEqual(0, parser.Errors.Count);
            Assert.IsTrue(arg.IsValid);
            Assert.IsInstanceOfType(arg, typeof(CssArithmeticOperation));
            Assert.AreEqual("30%+10%", arg.ToString());

            parser.SetContext("(30%+10%)");
            arg = parser.DoParse();
            Assert.IsTrue(parser.End);
            Assert.AreEqual(0, parser.Errors.Count);
            Assert.IsTrue(arg.IsValid);
            Assert.IsInstanceOfType(arg, typeof(CssArithmeticOperation));
            Assert.AreEqual("(30%+10%)", arg.ToString());
        }

        [TestMethod]
        public void WithParenthesis()
        {
            var parser = new CssValueParser();
            parser.SetContext("(30%+10%)*3");
            var arg = parser.DoParse();
            Assert.IsTrue(parser.End);
            Assert.AreEqual(0, parser.Errors.Count);
            Assert.IsTrue(arg.IsValid);
            Assert.IsInstanceOfType(arg, typeof(CssArithmeticOperation));
            Assert.AreEqual("(30%+10%)*3", arg.ToString());
        }

        [TestMethod]
        public void Priority()
        {
            var parser = new CssValueParser();
            parser.SetContext("30%/fn(2,3)+10%*1.4+2*3");
            var arg = parser.DoParse();
            Assert.IsTrue(parser.End);
            Assert.AreEqual(0, parser.Errors.Count);
            Assert.IsTrue(arg.IsValid);
            Assert.IsInstanceOfType(arg, typeof(CssArithmeticOperation));
            Assert.AreEqual("30%/fn(2,3)+10%*1.4+2*3", arg.ToString());

            Assert.AreEqual("30%/fn(2,3)", ((CssArithmeticOperation)arg).Left.ToString());
            Assert.AreEqual("10%*1.4+2*3", ((CssArithmeticOperation)arg).Right.ToString());

            var o = ((CssArithmeticOperation)arg).Right;
            Assert.IsInstanceOfType(o, typeof(CssArithmeticOperation));
            Assert.AreEqual("10%*1.4", ((CssArithmeticOperation)o).Left.ToString());
            Assert.AreEqual("2*3", ((CssArithmeticOperation)o).Right.ToString());
        }

        [TestMethod]
        public void WithSpaces()
        {
            var parser = new CssSimpleValueParser();
            parser.SetContext("x(abc, de fg )");
            var arg = parser.DoParse();
            Assert.IsTrue(parser.End);
            Assert.AreEqual(0, parser.Errors.Count);
            Assert.IsTrue(arg.IsValid);
            Assert.AreEqual(3, arg.Arguments.Count);
            Assert.AreEqual("x(abc,de fg)", arg.ToString());
        }
    }
}
