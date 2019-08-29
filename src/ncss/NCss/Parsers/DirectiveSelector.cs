using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable once CheckNamespace
namespace NCss
{
    public sealed class DirectiveSelector : Selector
    {
        internal override void AppendTo(StringBuilder sb)
        {
            sb.Append("@");
            if (!string.IsNullOrWhiteSpace(Name))
                sb.Append(Name);
            if (!string.IsNullOrWhiteSpace(Arguments))
            {
                sb.Append(' ');
                sb.Append(Arguments.Trim());
            }
        }

        public override bool IsValid
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Name);
            }
        }

        public string Name { get; set; }
        public string Arguments { get; set; }

        internal Rule.BodyType ExpectedBodyType
        {
            get
            {
                // http://realworldvalidator.com/css/atdirectives
                switch (Name)
                {
                    case "charset":
                    case "import":
                    case "namespace":
                        return Rule.BodyType.None;

                    case "keyframes":
                    case "-moz-keyframes":
                    case "-o-keyframes":
                    case "-webkit-keyframes":
                        // todo: handle that properly, since the child rules are not really 'rules'
                        // => Their selectors may be invalid names like '0%' '10%'
                        return Rule.BodyType.ChildRules;

                    case "host":
                    case "media":
                    case "supports":
                    case "document": // does not exist... someday ?
                    case "-moz-document":
                        return Rule.BodyType.ChildRules;
                    case "page":
                    case "counter":
                    case "font-face":
                    case "string":
                    case "viewport":
                    case "-moz-viewport":
                    case "-ms-viewport":
                    case "-o-viewport":
                    case "-webkit-viewport":
                        return  Rule.BodyType.Properties;
                    case "bottom-center":
                    case "bottom-left":
                    case "bottom-left-corner":
                    case "bottom-right":
                    case "bottom-right-corner": 
                    case "left-bottom":
                    case "left-middle":
                    case "left-top":
                    case "right-bottom":
                    case "right-middle":
                    case "right-top":
                    case "top-center":
                    case "top-left":
                    case "top-left-corner":
                    case "top-right":
                    case "top-right-corner":
                        return Rule.BodyType.Properties;

                        
                    default :
                    case "-we-palette":  // cant find anything about it
                        return Rule.BodyType.None;
                }
            }
        }

        public override Selector Clone(Predicate<Selector> filter)
        {
            if (filter != null && !filter(this))
                return null;
            var s = new DirectiveSelector
            {
                Arguments = Arguments,
                Name = Name,
            };
            s.SetParsingSource(this);
            return s;
        }
    }



    // ================================================================================================
    // ========================================= PARSER ===============================================
    // ================================================================================================


    namespace Parsers
    {

        internal class DirectiveSelectorParser : Parser<DirectiveSelector>
        {
            internal override DirectiveSelector DoParse()
            {
                if (CurrentChar != '@')
                    return null; // should never happen
                Index++;
                var name = PickName();
                var args = SkipUntil(';', '{');
                return new DirectiveSelector
                {
                    Name = string.IsNullOrWhiteSpace(name) ? null : name,
                    Arguments = string.IsNullOrWhiteSpace(args) ? null : args, // todo parse directive arguments
                };
            }
        }
    }
}