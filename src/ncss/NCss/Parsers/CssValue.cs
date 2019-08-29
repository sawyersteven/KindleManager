using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

// ReSharper disable once CheckNamespace
namespace NCss
{
    public abstract class CssValue : CssToken
    {
        public bool HasParenthesis { get; set; }
        public bool HasComma { get; set; }

        public abstract IEnumerable<TokenReference<T>> Find<T>(Predicate<T> matching) where T:CssToken;

        public static implicit operator string(CssValue val)
        {
            return val == null ? null : val.ToString();
        }

        protected abstract CssValue CloneInternal();

        public CssValue Clone()
        {
            var cl = CloneInternal();
            if (ReferenceEquals(cl, this))
                return cl;
            cl.SetParsingSource(this);
            cl.HasComma = HasComma;
            cl.HasParenthesis = HasParenthesis;
            return cl;
        }
    }

    public class CssArithmeticOperation : CssValue
    {
        internal override void AppendTo(StringBuilder sb)
        {
            if (HasParenthesis)
                sb.Append('(');
            if (Left != null)
                Left.AppendTo(sb);
            sb.Append(Operation);
            if (Right != null)
                Right.AppendTo(sb);
            if (HasParenthesis)
                sb.Append(')');
        }

        public override bool IsValid
        {
            get
            {
                return Validops.Contains(Operation)
                       && Left != null && Left.IsValid
                       && Right != null && Right.IsValid;
            }
        }

        internal static readonly char[][] OperatorsByPriority = new[] {new[] {'*', '/'}, new[] {'+', '-'}, new[] {'='}};
        internal static readonly char[] Validops = OperatorsByPriority.SelectMany(x => x).ToArray();
        public static readonly ReadOnlyCollection<char> ValidOperators = new ReadOnlyCollection<char>(Validops);

        public CssValue Left { get; set; }
        public CssValue Right { get; set; }
        public char Operation { get; set; }

        public override IEnumerable<TokenReference<T>> Find<T>(Predicate<T> matching)
        {
            if (Left != null)
            {
                var asT = Left as T;
                if (asT != null && matching(asT))
                {
                    yield return new TokenReference<T>(asT, () => Left = null, by => Left = by as CssValue);
                }
                else
                {
                    foreach (var m in Left.Find(matching))
                        yield return m;
                }

            }
            if (Right != null)
            {
                var asT = Right as T;
                if (asT != null && matching(asT))
                {
                    yield return new TokenReference<T>(asT, () => Right = null, by => Right = by as CssValue);
                }
                else
                {
                    foreach (var m in Right.Find(matching))
                        yield return m;
                }

            }
        }

        protected override CssValue CloneInternal()
        {
            var v = new CssArithmeticOperation
            {
                Left = Left == null ? null : Left.Clone(),
                Right = Right == null ? null : Right.Clone(),
                Operation = Operation,
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
        internal class CssValueParser : Parser<CssValue>
        {
            internal override CssValue DoParse()
            {
                if (End || CurrentChar == ')')
                    return null;

                var expectingClosingBrace = CurrentChar == '(';

                CssValue val;
                if (expectingClosingBrace)
                {
                    // we've a value in parenthesis like "(my value or inner op)" ... let's parse it.
                    Index++;
                    val = Parse<CssValue>();
                }
                else
                {
                    // we have a classic value like '0px' or a function call
                    val = Parse<CssSimpleValue>();
                }

                // check that we really have a value parsed
                if (val == null)
                {
                    if (expectingClosingBrace)
                    {
                        if (End || CurrentChar != ')')
                            AddError(ErrorCode.ExpectingToken, ")");
                        else
                            Index++;
                    }
                    return null;
                }


                // if this was an inner value, let's close the corresponding parenthesis
                if (expectingClosingBrace)
                {
                    val.HasParenthesis = true;
                    if (End || CurrentChar != ')')
                        AddError(ErrorCode.ExpectingToken, ")");
                    else
                        Index++;
                }

                // check other values
                var lst = new List<Tuple<char, CssValue>>
                {
                    Tuple.Create('_', val)
                };


                // arithmetic operation on other values (if any)
                CssValue operand = val;
                while (!End && !operand.HasComma)
                {
                    bool stop = false;
                    var c = CurrentChar;
                    if (CssArithmeticOperation.Validops.Contains(c))
                    {
                        if (WasWhitespace && NextChar != ' ')
                        {
                            // if we have something like "0px -5px", that's not an operation. "0px-5px" or "0px - 5px" are, though.
                            break;
                        }
                        Index++;
                        var v = Parse<CssSimpleValue>();
                        if (v == null)
                        {
                            AddError(ErrorCode.ExpectingValue, "operand");
                            break;
                        }
                        operand = v;
                        lst.Add(Tuple.Create(c, operand));
                    }
                    else
                        break;
                }

                // that's calculus ! let's parse the resulting operation.
                if (lst.Count > 1)
                {
                    // prioritize grouping values by operator precedence
                    foreach (var ops in CssArithmeticOperation.OperatorsByPriority)
                    {
                        for (int i = lst.Count - 1; i > 0; i--)
                        {
                            if (ops.Contains(lst[i].Item1))
                            {
                                lst[i - 1] = Tuple.Create(lst[i - 1].Item1, (CssValue) new CssArithmeticOperation
                                {
                                    Left = lst[i - 1].Item2,
                                    Right = lst[i].Item2,
                                    Operation = lst[i].Item1
                                });
                                lst.RemoveAt(i);
                                //i--;
                            }
                            if (lst.Count == 1)
                                break;
                        }
                        if (lst.Count == 1)
                            break;
                    }
                    if (lst.Count != 1)
                        throw new ParsingException("Cannot detect operator precedence");
                    val = lst[0].Item2;
                }

                // ended with a comma ? (arguments does, or font property arguments for instance) 
                val.HasComma = operand.HasComma;
                return val;
            }
        }
    }
}