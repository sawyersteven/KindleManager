using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NCss.Parsers;

namespace NCss
{
    public class ParsingException : Exception
    {
        public ParsingException(string message) : base(message) { }
    }

    public class BlockMismatchException : ParsingException
    {
        public BlockMismatchException() : base("Block mismatch") { }
    }

    [DebuggerStepThrough]
    static class ParserList
    {
        internal static readonly Dictionary<Type, Func<ParserBase>> Factories = new Dictionary<Type, Func<ParserBase>>
        {
            {typeof(Stylesheet), ()=>new StylesheetParser()},
            {typeof(Selector), ()=>new SelectorParser()},
            {typeof(DirectiveSelector), ()=>new DirectiveSelectorParser()},
            {typeof(SimpleSelector), ()=>new SimpleSelectorParser()},
            {typeof(ChildSelector), ()=>new ChildSelectorParser()},
            {typeof(InvalidSelector), ()=>new InvalidSelectorParser()},
            {typeof(AttributeCondition), ()=>new AttributeConditionParser()},
            {typeof(CssSimpleValue), ()=>new CssSimpleValueParser()},
            {typeof(CssValue), ()=>new CssValueParser()},

            {typeof(Rule), ()=>new RuleParser()},
            {typeof(Property), ()=>new PropertyParser()},
        };

        
    }
    
    abstract class ParserBase
    {
        string css;
        int _index;
        bool nestedBlock;
        bool wasWhitespace;


        protected abstract object DoParsePrivate();


        [DebuggerStepThrough]
        protected TElement Parse<TElement>(bool beginBlockDoNotUse=false)
            where TElement : CssToken
        {
            Func<ParserBase> parser;
            if (!ParserList.Factories.TryGetValue(typeof(TElement), out parser))
                throw new ParsingException("Cannot parse " + typeof (TElement) + ": No parser defined");
            Skip();
            if (End)
                return null;
            TElement ret = null;
            var startIndex = Index;
            var p = parser();
            // copy context
            p.SetContext(this.css, Index);
            p.wasWhitespace = wasWhitespace;
            p.nestedBlock = nestedBlock || beginBlockDoNotUse;
            try
            {
                ret = (TElement) p.DoParsePrivate();
            }
            catch (ParsingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // todo : safe mode... resume parsing somewhere safe. + error reporting
                throw;
            }
            finally
            {

                // copy context back
                _index = p._index;
                wasWhitespace = p.wasWhitespace;
                Errors.AddRange(p.Errors);

                var endIndex = Index;
                if (ret != null)
                    ret.SetParsingSource(this.css, startIndex, endIndex,p.Errors);
                Skip();
            }
            return ret;
        }

        
        internal List<T> ParseBlock<T>(bool startBlock)
            where T:CssBlockToken
        {
            if (End)
            {
                AddError(ErrorCode.UnexpectedEnd, "Expecting body");
            }

            var startIndex = Index;
            if (startBlock && CurrentChar != '{')
            {
                AddError(ErrorCode.ExpectingToken, "{");
            }

            var expectBlockEnd = startBlock && CurrentChar == '{';
            if (expectBlockEnd)
                _index++;
            Skip();
            ResetWasWhitespace();
            if (!expectBlockEnd && typeof(T) == typeof(Property))
            {
                // should never happen
                throw new ParsingException("Cannot parse properties in a block not surounded by braces {}");
            }
            
            var block = new List<T>();
            int currentTokenIndex = _index;
            try
            {
                while (true)
                {
                    if (CurrentChar == '}')
                    {
                        _index++;
                        Skip();
                        ResetWasWhitespace();
                        // expected block
                        if (nestedBlock)
                            return block;
                        // parasite '}' ... let's continue parsing till we die
                        if (!expectBlockEnd)
                        {
                            var err = AddError(ErrorCode.UnexpectedToken, "}");

                            if (typeof(T) == typeof(Rule) || typeof(Rule).IsAssignableFrom(typeof(T)))
                            {
                                var br = new NotParsableBlockRule();
                                br.SetParsingSource(css, _index-1, _index, new List<CssParsingError> { err });
                                block.Add((T)(object)br);
                            }
                            else if (typeof(T) == typeof(Property) || typeof(Property).IsAssignableFrom(typeof(T)))
                            {
                                var pr = new NotParsableProperty();
                                pr.SetParsingSource(css, _index-1, _index, new List<CssParsingError> { err });
                                block.Add((T)(object)pr);
                            }
                        }
                        else // effective block end
                            return block;
                    }

                    if (!End)
                    {
                        currentTokenIndex = _index;
                        var rule = Parse<T>(true);
                        if (rule == null)
                            _index = currentTokenIndex;
                        else
                            block.Add(rule);
                    }

                    if (currentTokenIndex == _index)
                    {
                        var oi = _index;
                        if(!Skip("<!--") && !Skip("-->"))
                            _index++;
                        if (typeof (T) == typeof (Rule) || typeof (Rule).IsAssignableFrom(typeof (T)))
                        {
                            CssBlockToken bl = block.LastOrDefault() as NotParsableBlockRule;
                            if (bl == null)
                            {
                                var err = AddError(ErrorCode.UnexpectedToken, css.Substring(oi,_index-oi));
                                bl = new NotParsableBlockRule();
                                bl.SetParsingSource(css, oi, _index, new List<CssParsingError> { err });
                                block.Add((T) (object) bl);
                            }
                            else
                                bl.SetParsingSource(css, bl.FromIndex, _index, bl.Errors);
                        }
                        else if (typeof (T) == typeof (Property) || typeof (Property).IsAssignableFrom(typeof (T)))
                        {
                            NotParsableProperty bl = block.LastOrDefault() as NotParsableProperty;
                            if (bl == null)
                            {
                                var err = AddError(ErrorCode.UnexpectedToken, css.Substring(oi, _index - oi));
                                bl = new NotParsableProperty();
                                bl.SetParsingSource(css, _index - 1, _index, new List<CssParsingError> { err });
                                block.Add((T)(object)bl);
                            }
                            else
                                bl.SetParsingSource(css, bl.FromIndex, _index, bl.Errors);
                        }
                    }

                    if (End)
                    {
                        if (expectBlockEnd)
                            AddError(ErrorCode.ExpectingToken, "}");
                        return block;
                    }
                }
            }
            catch (BlockMismatchException)
            {
                var err = AddError(ErrorCode.BlockMismatch, "unexpected block token");

                var invalid = css.Substring(currentTokenIndex, _index - currentTokenIndex);

                if (string.IsNullOrWhiteSpace(invalid))
                {
                    // block mismatched, without any change to index... let's force to move on !
                    // this must be deeply invalid css
                    _index++;
                    return block; 
                }

                if (typeof(T) == typeof(Rule) || typeof(Rule).IsAssignableFrom(typeof(T)))
                {
                    var br = new NotParsableBlockRule();
                    br.SetParsingSource(css, currentTokenIndex, _index, new List<CssParsingError> { err });
                    block.Add((T)(object)br);
                }
                else if (typeof(T) == typeof(Property) || typeof(Property).IsAssignableFrom(typeof(T)))
                {
                    var pr = new NotParsableProperty();
                    pr.SetParsingSource(css, currentTokenIndex, _index, new List<CssParsingError> { err });
                    block.Add((T) (object) pr);
                }
                else
                    throw new ParsingException("No handler for " + typeof (T));

                return block;
            }
        }

        [DebuggerStepThrough]
        internal void SetContext(string css, int index=0)
        {
            this.css = css;
            this._index = index;
            wasWhitespace = false;
            Errors.Clear();
        }


        internal bool End
        {
            [DebuggerStepThrough]
            get { return Index >= css.Length; }
        }

        internal char CurrentChar
        {
            [DebuggerStepThrough]
            get { return css[Index]; }
        }


        protected char? NextChar
        {
            [DebuggerStepThrough]
            get { return Index>=(css.Length-1)? (char?) null : css[Index+1]; }
        }

        protected int Index
        {
            [DebuggerStepThrough]
            get { return _index; }
            [DebuggerStepThrough]
            set
            {
                if (_index > value)
                    throw new ParsingException("Cannot rewind");
                var nv = value > css.Length ? css.Length : value;

                // implictely skip comments
                int x;
                for (x = _index; x < nv; x++)
                {
                    var c = css[x];
                    if (c == '}' || c == '{')
                    {
                        _index = x;
                        throw new BlockMismatchException(); // todo: ParseBlock<> that handles this safely (and replaces block content with a comment)
                    }
                    if (c == '/' && x < css.Length - 1 && css[x + 1] == '*')
                    {
                        x++; // skip the '/'
                        do
                        {
                            x++;
                            if (x >= css.Length-1)
                            {
                                AddError(ErrorCode.ExpectingToken, "*/");
                                _index = css.Length;
                                return;
                            }
                        } while (css[x] != '*' || css[x+1] != '/');
                        wasWhitespace = true;
                        x ++; // skip the '*/'
                    }
                }

                _index = x;
                wasWhitespace = false;
                Skip();
            }
        }

        /// <summary>
        /// Was a space skipped after the last pick (name,...)
        /// </summary>
        protected bool WasWhitespace
        {
            get
            {
                return wasWhitespace;
            }
        }
        protected void ResetWasWhitespace()
        {
            wasWhitespace = false;
        }

        protected bool IsNameStart
        {
            get
            {
                if (End)
                    return false;
                return Regex.IsMatch(CurrentChar.ToString(), @"[A-Za-zÀ-ÿ\-_]");
            }
        }


        protected string PickString()
        {
            var nm = PickNameOrNumber();
            if (nm != null || End)
                return nm;
            var separator = CurrentChar;
            if (separator != '\'' && separator != '"')
                return null;
            if (NextChar == null)
            {
                AddError(ErrorCode.UnexpectedEnd, separator.ToString());
                Index++;
                return separator.ToString();
            }
            var sb = new StringBuilder();
            char c = separator;
            do
            {
                sb.Append(c);
                _index++;
                // skip escapes
                while (c == '\\')
                {
                    if (End)
                    {
                        AddError(ErrorCode.UnexpectedEnd, separator.ToString());
                        return sb.ToString();
                    }
                    sb.Append(c = CurrentChar);
                    _index++;
                    if (c == '\r' || c == '\n')
                        break;
                }
                if (c == '\r' || c == '\n')
                {
                    AddError(ErrorCode.UnexpectedLineBreak, "in string");
                    while (!End && (c = CurrentChar) == '\r' || c == '\n')
                        _index++;
                    wasWhitespace = true;
                    return sb.ToString();
                }
            } while (!End && (c = CurrentChar) != separator);

            if (End)
            {
                AddError(ErrorCode.UnexpectedEnd, separator.ToString());
                return sb.ToString();
            }
            Index++; // skip last separator
            sb.Append(separator);
            return sb.ToString();
        }

        protected string PickNameOrNumber()
        {
            var ret = PickName();
            if (ret != null)
                return ret;
            return PickNumber();
        }

        protected string PickName()
        {
            if (End)
                return null;
            Skip();
            var from = Index;

            if (!IsNameStart)
                return null;
            if (CurrentChar == '-')
            {
                var nc = NextChar;
                if (nc.HasValue)
                {
                    if (!Regex.IsMatch(nc.ToString(), @"[A-Za-zÀ-ÿ\-_]"))
                        return null;
                    _index += 2; // skip the first two
                }
                else
                    return null;
            }
            char c;
            while (!End && Regex.IsMatch(CurrentChar.ToString(), @"[A-Za-zÀ-ÿ\-_0-9]"))
            {
                _index++;
            }
            if (from == Index)
                return null;
            var to = Index;
            wasWhitespace = false;
            Skip();
            return css.Substring(from, to - from);
        }

        protected string PickValue()
        {
            if (!End && CurrentChar == '#')
                return PickHexColor();
            var n = PickNumberWithUnit();
            if (n != null)
                return n;
            n = PickNameOrNumber();
            if (n != null)
                return n;
            n = PickString();
            return n;
        }

        protected string PickHexColor()
        {
            if (End || CurrentChar != '#')
                return null;
            var oi = _index;
            _index++;
            // we're being permissive on how many chars they must be, and which they must be
            var sb = new StringBuilder();
            char c;
            while (!End && ((c=CurrentChar) >= '0' && c <='9' || c>='a' && c<='z' || c>='A' && c<='Z'))
            {
                sb.Append(c);
                _index++;
            }
            var nm = sb.ToString();
            if (nm == "")
            {
                _index = oi;
                return null;
            }
            Skip();
            return "#" + nm;
        }


        static readonly string[] units = { "%" , "px", "pt" // most used first
            ,"em", "mm", "cm"
            , "deg", "rem", "grad", "rad", "turn", "ex", "hz", "in", "khz", "ms", "s", "pc", "vw", "vh", "vmin", "vmax", "n" };

        protected string PickNumberWithUnit(params string[] onlyUnits)
        {
            if (onlyUnits == null || onlyUnits.Length == 0)
                onlyUnits = units;
            var oi = _index;
            var pn = PickNumber();
            if (pn == null)
                return null;
            if (End)
            {
                _index = oi;
                return null;
            }
            foreach (var u in onlyUnits)
            {
                bool ok = true;
                for (int i = 0; i < u.Length; i++)
                {
                    if (_index + i >= css.Length)
                    {
                        ok = false;
                        break;
                    }

                    if (u[i] != css[_index + i])
                    {
                        ok = false;
                        break;
                    }
                }

                if (ok)
                {
                    _index += u.Length;
                    wasWhitespace = false;
                    Skip();
                    return pn + u;
                }
            }
            _index = oi;
            return null;
        }

        protected string PickNumber()
        {
            if (End)
                return null;
            Skip();
            var oi = _index;
            bool hadDigit = false;
            var b = new StringBuilder();

            if (CurrentChar == '-')
            {
                var nc = NextChar;
                if (nc.HasValue && nc == '.' || (nc >= '0' && nc <= '9'))
                {
                    b.Append('-');
                    _index++;
                }
                else
                {
                    _index = oi;
                    return null;
                }
            }

            if (CurrentChar == '.')
            {
                if (!(NextChar >= '0' && NextChar <= '9')) // that's a nullable. do not replce by ||
                {
                    _index = oi;
                    return null;
                }
                hadDigit = true;
                b.Append("0.");
                _index++;
            }

            char c;
            while (!End && (
                            (c = css[_index]) >= '0' && c <= '9'
                            || !hadDigit && c =='.'
                        )
                        )
            {
                if (c == '.')
                    hadDigit = true;
                b.Append(c);
                _index++;
            }

            var val = b.ToString();
            if (string.IsNullOrEmpty(val))
            {
                _index = oi;
                return null;
            }
            wasWhitespace = false;
            Skip(); //after was whitespace reset ? think so yes.
            return val;
        }

        protected bool Skip(string str)
        {
            if (_index + str.Length > css.Length)
                return false;
            var oi = _index;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] != css[_index])
                {
                    _index = oi;
                    return false;
                }
                _index++;
            }
            Skip();
            return true;
        }

        [DebuggerStepThrough]
        protected void Skip()
        {
            while (SkipSpaces() || SkipComment())
            {
                wasWhitespace = true;
            }
        }
        //[DebuggerStepThrough]
        protected string SkipUntil(params char[] any)
        {
            var sb = new StringBuilder();
            if (SkipComment() && !End && !char.IsWhiteSpace(CurrentChar))
                sb.Append(' ');
            char c = '\0';
            while (!End && !any.Contains((c = CurrentChar)))
            {
                if (CurrentChar == '"' || CurrentChar == '\'')
                {
                    sb.Append(PickString());
                    if (WasWhitespace)
                        sb.Append(' ');
                    ResetWasWhitespace();
                    continue;
                }
                sb.Append(c);
                _index++;
                if (SkipComment() && !char.IsWhiteSpace(c) && !char.IsWhiteSpace(CurrentChar))
                    sb.Append(' ');
            }
            wasWhitespace = char.IsWhiteSpace(c);
            return sb.ToString();
        }

        [DebuggerStepThrough]
        bool SkipSpaces()
        {
            if (End)
                return false;
            if (!char.IsWhiteSpace(CurrentChar))
                return false;
            do
            {
                _index++;
            } while (!End && char.IsWhiteSpace(CurrentChar));
            return true;
        }

        //[DebuggerStepThrough]
        bool SkipComment()
        {
            if (End)
                return false;

            if (CurrentChar != '/' || NextChar != '*')
                return false;

            // comments acts lilke whitespaces
            wasWhitespace = true;

            // skip /*
            _index += 2;
            while (!End && (CurrentChar != '*' || NextChar != '/'))
                _index++;

            if (!End && CurrentChar == '*' && NextChar == '/')
            {
                // skip */
                _index += 2;
                return true;
            }

            AddError(ErrorCode.UnexpectedEnd, "*/");
            return false;
        }

        public readonly List<CssParsingError> Errors = new List<CssParsingError>();
        protected CssParsingError AddError(ErrorCode code, string details)
        {
            var err = new CssParsingError(code, details, _index, ToString());
            Errors.Add(err);
            return err;
        }

        public sealed override string ToString()
        {
            const int maxlen = 150;
            if (Index >= css.Length)
            {
                var from = css.Length - maxlen - 1;
                if(from<=0)
                    return css + "▮";
                return "..." + css.Substring(from) + "▮";
            }

            if (Index - maxlen/2 < 0)
            {
                if (Index + 1 + maxlen - Index >= css.Length)
                    return css.Substring(0, Index) + "▶" + css[Index] + "◀" + css.Substring(Index + 1);
                else
                    return css.Substring(0, Index) + "▶" + css[Index] + "◀" + css.Substring(Index + 1, maxlen/2 - Index) + "...";
            }
            if (Index + 1 + maxlen / 2 >= css.Length)
                return "..." + css.Substring(Index - maxlen / 2, maxlen / 2) + "▶" + css[Index] + "◀" + css.Substring(Index + 1);
            else
                return "..." + css.Substring(Index - maxlen / 2, maxlen / 2) + "▶" + css[Index] + "◀" + css.Substring(Index + 1, maxlen / 2) + "...";
        }
    }


    public enum ErrorCode
    {
        ExpectingToken,
        UnexpectedToken,
        UnexpectedEnd,
        ExpectingValue,
        ExpectingBody,
        BlockMismatch,
        UnexpectedLineBreak
    }
}
