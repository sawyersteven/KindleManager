using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable once CheckNamespace
namespace NCss
{

    public class SimpleSelector : Selector
    {
        public SimpleSelector() { }

        public SimpleSelector(string fulname)
        {
            FullName = fulname;
        }
        public string FullName
        {
            get
            {
                if (SelectorType == Type.PseudoElement)
                    return "::" + Name;
                if (SelectorType > 0)
                    return ((char) SelectorType) + Name;
                return Name;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    Name = null;
                    selectorType = Type.ElementType;
                    return;
                }
                if (value.StartsWith("::"))
                {
                    selectorType = Type.PseudoElement;
                    Name = value.Substring(2);
                    return;
                }

                if (value.EndsWith("%"))
                {
                    selectorType = Type.Percentage;
                    Name = value.Substring(0, value.Length - 1);
                    return;
                }

                if(Enum.IsDefined(typeof(Type), (int)value[0]))
                {
                    selectorType = (Type) value[0];
                    Name = value.Substring(1);
                    return;
                }
                selectorType = Type.ElementType;
                Name = value;
            }
        }
        public enum Type
        {
            ElementType = -1,
            Percentage = -2, // for keyframes selector
            PseudoElement = -3,
            All = (int)'*',
            Id = (int)'#',
            Class = (int)'.',
            PseudoClass = (int)':',
        }

        Type selectorType;

        public Type SelectorType
        {
            get { return selectorType; }
            set
            {
                switch (value)
                {
                    case Type.Percentage:
                        prefix = "";
                        break;
                    case Type.ElementType:
                        prefix = "";
                        break;
                    case Type.PseudoElement:
                        prefix = "::";
                        break;
                    case Type.All:
                    case Type.Id:
                    case Type.Class:
                    case Type.PseudoClass:
                        prefix = ((char) value).ToString();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("value");
                }
                selectorType = value;
            }
        }

        string prefix;

        public string SelectorPrefix
        {
            get { return prefix; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    prefix = "";
                    selectorType = Type.ElementType;
                    return;
                }
                value = value.Trim();
                if (value == "::")
                {
                    prefix = "::";
                    selectorType = Type.PseudoElement;
                    return;
                }

                if (value.Length == 1 && Enum.IsDefined(typeof (SimpleSelector), value[0]))
                {
                    selectorType = (Type) value[0];
                    prefix = value;
                    return;
                }
                selectorType = Type.ElementType;
                prefix = value;
            }
        }

        public string Name { get; set; }

        public bool HasArgument
        {
            get { return argument != null || selectorArgument != null; }
        }

        CssValue argument;

        public CssValue Argument
        {
            get
            {
                return argument;
            }
            set
            {
                argument = value;
                if(value != null)
                    selectorArgument = null;
            }
        }

        Selector selectorArgument;

        public Selector SelectorArgument
        {
            get
            {
                return selectorArgument;
            }
            set
            {
                selectorArgument = value;
                if (value != null)
                    argument = null;
            }
        }


        internal override void AppendTo(StringBuilder sb)
        {
            sb.Append(FullName);
            if (HasArgument)
            {
                sb.Append('(');
                if (SelectorArgument != null)
                    SelectorArgument.AppendTo(sb);
                else
                    sb.Append(Argument);
                sb.Append(')');
            }
        }

        public override bool IsValid
        {
            get
            {
                if (!Enum.IsDefined(typeof (Type), this.selectorType))
                    return false;
                if (string.IsNullOrWhiteSpace(Name) && this.selectorType != Type.All)
                    return false;
                if (!HasArgument)
                    return true;
                if (SelectorArgument != null)
                    return SelectorArgument.IsValid;
                return Argument.IsValid;
            }
        }

        public override IEnumerable<TokenReference<T>> Find<T>(Predicate<T> matching)
        {
            if (Argument != null)
            {
                var ast = Argument as T;
                if (ast != null && matching(ast))
                    yield return new TokenReference<T>(ast, () => Argument = null, by => Argument = by as CssValue);
                else
                {
                    foreach (var m in Argument.Find(matching))
                        yield return m;
                }
            }

            if (SelectorArgument != null)
            {
                var ast = SelectorArgument as T;
                if (ast != null && matching(ast))
                    yield return new TokenReference<T>(ast, () => SelectorArgument = null, by => SelectorArgument = by as Selector);
                else
                {
                    foreach (var m in SelectorArgument.Find(matching))
                        yield return m;
                }
            }
        }

        public override Selector Clone(Predicate<Selector> filter)
        {
            if (filter != null)
            {
                if (selectorArgument != null)
                {
                    if (!filter(selectorArgument) && !filter(this))
                        return null;
                }
                else if (!filter(this))
                    return null;
            }
            var s = new SimpleSelector
            {
                Name = Name,
                selectorArgument = selectorArgument == null ? null : selectorArgument.Clone(null),
                argument = argument == null ? null : argument.Clone(),
                SelectorType = SelectorType,
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

        internal class SimpleSelectorParser : Parser<SimpleSelector>
        {

            internal override SimpleSelector DoParse()
            {
                SimpleSelector.Type? type = null;
                string name = null;
                if (Enum.IsDefined(typeof (SimpleSelector.Type), (int) CurrentChar))
                {
                    type = (SimpleSelector.Type) CurrentChar;
                    Index++;
                }
                else if (IsNameStart)
                {
                    type = SimpleSelector.Type.ElementType;
                }
                else
                {
                    var percents = PickNumberWithUnit("%");
                    if (percents != null)
                    {
                        type = SimpleSelector.Type.Percentage;
                        name = percents;
                    }
                }

                if (!End && type == SimpleSelector.Type.PseudoClass && CurrentChar == ':')
                {
                    type = SimpleSelector.Type.PseudoElement;
                    Index++;
                }

                if (type == SimpleSelector.Type.All)
                {
                    return new SimpleSelector {SelectorType = SimpleSelector.Type.All};
                }

                if (type == null)
                    return null;

                if (name == null)
                    name = PickName();
                if (name == null)
                {
                    AddError(ErrorCode.ExpectingToken, "selector name");
                    return null;
                }

                if (!WasWhitespace && !End && CurrentChar == '(')
                {
                    Index++; // skip '('
                    // handle :lang(en) or :not(.selector)
                    Selector sel = null;
                    CssValue arg = null;
                    if (name == "not")
                        sel = Parse<Selector>();
                    else
                        arg = Parse<CssValue>();

                    if (arg == null && sel == null)
                        AddError(ErrorCode.ExpectingValue, "argument");
                    
                    if (!End && CurrentChar == ')')
                        Index++;
                    return new SimpleSelector
                    {
                        Name = name,
                        SelectorType = type.Value,
                        SelectorArgument = sel,
                        Argument = arg,
                    };
                }

                return new SimpleSelector
                {
                    SelectorType = type.Value,
                    Name = name,
                };
            }
        }
    }
}