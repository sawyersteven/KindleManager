using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NCss.Parsers;

namespace NCss
{
    public class CssParser
    {

        public CssParser()
        {
            
        }

        public List<CssParsingError> Errors { get; private set; }

        public Stylesheet ParseSheet(string cssBody)
        {
            var parser = new StylesheetParser();
            parser.SetContext(cssBody);
            var ret = parser.DoParse();
            Errors = parser.Errors;
            return ret;
        }

    }
}
