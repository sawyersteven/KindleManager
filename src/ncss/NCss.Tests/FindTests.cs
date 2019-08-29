using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCss.Tests
{
    [TestClass]
    public class FindTests
    {
        [TestMethod]
        public void ReplaceHover()
        {
            var sheet = new CssParser().ParseSheet(".cl:hover,.cl2:hover{}.cl3:hover{}");
            var found = sheet.Find<Selector>(x => x ==":hover").ToArray();
            Assert.IsTrue(sheet.IsValid);
            Assert.AreEqual(3, found.Length);
            foreach (var f in found)
                f.ReplaceBy(new SimpleSelector(".hov"));
            Assert.AreEqual(".cl.hov,.cl2.hov{}.cl3.hov{}", sheet.ToString());
        }

        [TestMethod]
        public void RemoveHover()
        {
            var sheet = new CssParser().ParseSheet(".cl:hover,.cl2:hover{}.cl3:hover{}");
            foreach (var f in sheet.Find<SimpleSelector>(x => x.FullName == ":hover").ToArray())
                f.Remove();
            Assert.IsTrue(sheet.IsValid);
            Assert.AreEqual(".cl,.cl2{}.cl3{}", sheet.ToString());
        }

        [TestMethod]
        public void RemoveOneClass()
        {
            var sheet = new CssParser().ParseSheet(".cl1,.cl2{}");
            foreach (var f in sheet.Find<SimpleSelector>(x => x.FullName == ".cl2").ToArray())
                f.Remove();
            Assert.IsTrue(sheet.IsValid);
            Assert.AreEqual(".cl1{}", sheet.ToString());
        }

        [TestMethod]
        public void FindUrl()
        {
            var sheet = new CssParser().ParseSheet(".cl{background-image:url(test.png)}");
            foreach (var f in sheet.Find<CssSimpleValue>(x => x.IsFunction && x.Name == "url").ToArray())
                f.ReplaceBy(new CssSimpleValue("url", "other.png"));
            Assert.IsTrue(sheet.IsValid);
            Assert.AreEqual(".cl{background-image:url(other.png);}", sheet.ToString());
        }

        [TestMethod]
        public void FindUrlAmongOthers()
        {
            var sheet = new CssParser().ParseSheet(".cl{background: transparent url(test.png)}");
            foreach (var f in sheet.Find<CssSimpleValue>(x => x.IsFunction && x.Name == "url").ToArray())
                f.ReplaceBy(new CssSimpleValue("url", "other.png"));
            Assert.IsTrue(sheet.IsValid);
            Assert.AreEqual(".cl{background:transparent url(other.png);}", sheet.ToString());
        }


        [TestMethod]
        public void RemoveClass()
        {
            var sheet = new CssParser().ParseSheet(".cl1{}.cl2#id{}");
            foreach (var f in sheet.Find<ClassRule>(x => x.Selector == ".cl2#id").ToArray())
                f.Remove();
            Assert.IsTrue(sheet.IsValid);
            Assert.AreEqual(".cl1{}", sheet.ToString());
        }

        [TestMethod]
        public void RemoveProperty()
        {
            var sheet = new CssParser().ParseSheet(".cl1{prop:red;other:null;}.cl2#id{prop:test;}");
            foreach (var f in sheet.Find<Property>(x => x.Name == "prop").ToArray())
                f.Remove();
            Assert.IsTrue(sheet.IsValid);
            Assert.AreEqual(".cl1{other:null;}.cl2#id{}", sheet.ToString());
        }

        [TestMethod]
        public void RemoveInMedia()
        {
            var sheet = new CssParser().ParseSheet("@media test{.cl:hover{bg:red;}}");
            foreach (var f in sheet.Find<Selector>(x => x == ":hover").ToArray())
                f.Remove();
            Assert.IsTrue(sheet.IsValid);
            Assert.AreEqual("@media test{.cl{bg:red;}}", sheet.ToString());
        }
    }
}
