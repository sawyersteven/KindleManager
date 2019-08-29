using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCss.Tests
{
    [TestClass]
    public class CloneTests
    {
        // NB: Most of the tests are performed via RealTests (cloned on each library tested)


        [TestMethod]
        public void ClonePercentSubRules()
        {
            var input = "@-webkit-keyframes AdsAssetSelector_highlight{0%{background-color:#fff;}}";
            var sheet = new CssParser().ParseSheet(input);
            var s2 = sheet.Clone();
            Assert.AreEqual(input, sheet.ToString());
            Assert.AreEqual(input, s2.ToString());
        }

        [TestMethod]
        public void FilterHovers()
        {
            var input = "@media test{.cl{prop:any}.cl:hover{prop:keep}}@media empty{.cl{}}.clrem{}.clkeep:hover{prop:keep}.clkeep, .other:hover{any:x}.clkeep .test:hover{any:y}.keep:not(:hover){any:z}";
            var expected = @"@media test{.cl:hover{prop:keep;}}.clkeep:hover{prop:keep;}.other:hover{any:x;}.clkeep .test:hover{any:y;}.keep:not(:hover){any:z;}";
            var sheet = new CssParser().ParseSheet(input);
            var got = sheet.Clone(x => x == ":hover");
            Assert.AreEqual(expected, got.ToString());
        }
    }
}
