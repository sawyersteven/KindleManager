using System;
using System.Text;
using System.Text.RegularExpressions;

// ReSharper disable once CheckNamespace
namespace NCss
{

    public class InvalidSelector : Selector
    {

        internal override void AppendTo(StringBuilder sb)
        {
        }

        public override bool IsValid
        {
            get { return false; }
        }

        public override Selector Clone(Predicate<Selector> filter)
        {
            if (filter != null)
                return null;
            return this; // i'm immutable :)
        }
    }



    // ================================================================================================
    // ========================================= PARSER ===============================================
    // ================================================================================================


    namespace Parsers
    {
        internal class InvalidSelectorParser : Parser<InvalidSelector>
        {

            internal override InvalidSelector DoParse()
            {
                var c = CurrentChar;
                int braced = 0;
                while (!Regex.IsMatch(c.ToString(), @"[\.\s#+>,~;{}]") || braced > 0)
                {
                    if (c == '(')
                        braced++;
                    if (braced > 0 && c == ')')
                        braced--;
                    //sb.Append(c);
                    Index++;
                    if (End)
                        break;
                    c = CurrentChar;
                }

                return new InvalidSelector();
            }
        }
    }
}