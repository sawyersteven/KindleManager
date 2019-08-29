using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NCss.Parsers;

// ReSharper disable once CheckNamespace
namespace NCss
{
    /// <summary>
    /// A simple css value, or a function call
    /// </summary>
    public class CssSimpleValue : CssValue
    {
        public CssSimpleValue() { }

        public CssValue Parse(string cssValue)
        {
            var parser = new CssValueParser();
            parser.SetContext(cssValue);
            var val = parser.DoParse();
            return val;
        }

        public CssSimpleValue(string name, params string[] arguments)
        {
            Name = name;
            if (arguments != null && arguments.Length > 0)
                Arguments = arguments.Select(x => (CssValue) new CssSimpleValue(x)).ToList();
        }

        public CssSimpleValue(string name, CssValue[] arguments)
        {
            Name = name;
            if (arguments != null && arguments.Length > 0)
                Arguments = arguments.ToList();
        }

        public string Name { get; set; }

        /// <summary>
        /// Provided if that's a function call
        /// </summary>
        public List<CssValue> Arguments { get; set; }

        public bool IsFunction { get { return Arguments != null && Arguments.Count > 0; } }

        internal override void AppendTo(StringBuilder sb)
        {
            if (HasParenthesis)
                sb.Append('(');
            if (!string.IsNullOrWhiteSpace(Name))
                sb.Append(Name);
            if (Arguments != null)
            {
                sb.Append('(');
                bool appendSpace = false;
                foreach (var a in Arguments)
                {
                    if (appendSpace)
                        sb.Append(' ');
                    a.AppendTo(sb);
                    appendSpace = !a.HasComma;
                }
                    
                sb.Append(')');
            }
            if (HasComma)
                sb.Append(',');
            if (HasParenthesis)
                sb.Append(')');
        }

        public override bool IsValid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name))
                    return false;
                if (Arguments != null)
                {
                    // check that it is a function name
                    if (Name.StartsWith("progid:"))
                    {
                        if (!Regex.IsMatch(Name, @"^progid:[a-zA-Z_\-][a-zA-Z0-9_\-\.]*$"))
                            return false;
                    }
                    else if (Name == "url")
                        return Arguments.Count == 1;
                    else if (!Regex.IsMatch(Name, @"^[a-zA-Z_\-][a-zA-Z0-9_\-]*$"))
                        return false;
                    return Arguments.All(a => a.IsValid);
                }

                if (Name[0] == '#')
                {
                    // is valid color ?
                    return Regex.IsMatch(Name, @"^#([a-fA-F0-9]{3}){1,2}$");
                }

                // is valid string ?
                if (Name[0] == '\'')
                    return Name.Length > 1 && Name[Name.Length - 1] == '\'';
                if (Name[0] == '"')
                    return Name.Length > 1 && Name[Name.Length - 1] == '"';

                return true;
            }
        }

        public override IEnumerable<TokenReference<T>> Find<T>(Predicate<T> matching)
        {
            if(Arguments == null)
                yield break;
            foreach (var a in Arguments)
            {
                var cv = a;
                var asT = a as T;
                if (asT != null && matching(asT))
                {
                    yield return new TokenReference<T>(asT, () => Arguments.Remove(cv), by =>
                    {
                        var i = Arguments.IndexOf(cv);
                        if (i >= 0)
                            Arguments[i] = by as CssValue;
                    });
                }
                else
                {
                    foreach (var subm in a.Find(matching))
                        yield return subm;
                }
            }
        }

        protected override CssValue CloneInternal()
        {
            var v = new CssSimpleValue
            {
                Arguments = Arguments == null ? null : Arguments.Select(x => x.Clone()).ToList(),
                Name = Name,
            };
            v.SetParsingSource(this);
            return v;
        }
    }



    // ================================================================================================
    // ========================================= PARSER ===============================================
    // ================================================================================================


    namespace Parsers
    {
        internal class CssSimpleValueParser : Parser<CssSimpleValue>
        {
            /// <summary>
            /// A "value" is a simple value, or a function call. It MAY end with a comma.
            /// </summary>
            internal override CssSimpleValue DoParse()
            {
                var value = PickValue();
                if (value == null)
                    return null;
                if (value == "progid" && !End && CurrentChar == ':')
                {
                    // ie is a shit
                    var fun = SkipUntil('(', ';', '}');
                    value = value + fun;
                }

                if (End)
                    return new CssSimpleValue {Name = value,};

                List<CssValue> argument = null;

                #region We've got a function call !

                if (CurrentChar == '(')
                {
                    Index++;
                    if (value.ToLower() == "url")
                    {
                        // url function is a bit special, since it can host data that may have ';' inside.
                        // it always contains raw values without line breaks, though.. let's not parse it's argument and read it from raw.
                        argument = new List<CssValue>
                        {
                            new CssSimpleValue
                            {
                                Name = SkipUntil(')', '\r', '\n', '}')
                            }
                        };
                    }
                    else
                    {
                        // Parse function arguments
                        argument = new List<CssValue>();
                        while (!End)
                        {
                            var na = Parse<CssValue>();
                            if (na == null)
                            {
                                AddError(ErrorCode.ExpectingToken, "argument");
                                break;
                            }
                            argument.Add(na);

                            if (!End && CurrentChar == ')')
                                break;
                        }
                    }

                    if (End)
                        AddError(ErrorCode.UnexpectedEnd, ")");
                    else if (CurrentChar != ')')
                        AddError(ErrorCode.ExpectingToken, ")");
                    else
                        Index++;
                }

                #endregion

                // is this value ending with a comma ?
                bool hasComma = !End && CurrentChar == ',';
                if (hasComma)
                    Index++;


                return new CssSimpleValue
                {
                    Name = value,
                    HasComma = hasComma,
                    Arguments = argument,
                };
            }


        }

    }
}