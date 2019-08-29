using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

// ReSharper disable once CheckNamespace
namespace NCss
{
    public abstract class Selector : CssToken
    {
        public virtual IEnumerable<TokenReference<T>> Find<T>(Predicate<T> matching) where T : CssToken
        {
            yield break;
        }


        public static implicit operator String(Selector s)
        {
            return s == null ? null : s.ToString();
        }

        public Selector Clone<T>(Predicate<T> filter) where T : Selector
        {
            return Clone(x =>
            {
                var ast = x as T;
                return ast != null && filter(ast);
            });
        }
        
        public abstract Selector Clone(Predicate<Selector> filter);
    }

    public class MultiConditionSelector : Selector
    {
        public List<Selector> Conditions { get; set; } = new List<Selector>();

        internal override void AppendTo(StringBuilder sb)
        {
            AppendToWithOptions(sb, CssRestitution.OnlyWhatYouUnderstood);
        }

        public override void AppendToWithOptions(StringBuilder sb, CssRestitution option)
        {
            foreach (var c in Conditions)
                c.AppendToWithOptions(sb, option);
        }

        public override IEnumerable<TokenReference<T>> Find<T>(Predicate<T> matching)
        {
            if(Conditions == null)
                yield break;
            foreach (var c in Conditions)
            {
                var vc = c;
                var ast = c as T;
                if (ast != null && matching(ast))
                    yield return new TokenReference<T>(ast, () => Conditions.Remove(vc), by =>
                    {
                        var i = Conditions.IndexOf(vc);
                        if (i >= 0)
                            Conditions[i] = by as Selector;
                    });
                else
                {
                    foreach (var sub in c.Find(matching))
                        yield return sub;
                }
            }
        }


        public override Selector Clone(Predicate<Selector> filter)
        {
            if (filter != null)
            {
                if (Conditions == null)
                    return null;
                if (Conditions.All(x => x.Clone(filter) == null))
                    return null;
            }
            var s = new MultiConditionSelector
            {
                Conditions = Conditions == null ? null : Conditions.Select(x => x.Clone(null)).ToList(),
            };
            s.SetParsingSource(this);
            return s;
        }

        public override bool IsValid
        {
            get { return Conditions.All(x => x.IsValid); }
        }
    }
    public class SelectorList : Selector
    {
        public List<Selector> Selectors { get; set; } = new List<Selector>();

        internal override void AppendTo(StringBuilder sb)
        {
            AppendToWithOptions(sb, CssRestitution.OnlyWhatYouUnderstood);
        }

        public override void AppendToWithOptions(StringBuilder sb, CssRestitution option)
        {
            bool notFirst = false;
            foreach (var s in Selectors)
            {
                if (notFirst)
                    sb.Append(',');
                notFirst = true;
                s.AppendToWithOptions(sb, option);
            }
        }

        public override IEnumerable<TokenReference<T>> Find<T>(Predicate<T> matching)
        {
            if(Selectors == null)
                yield break;
            foreach (var s in Selectors)
            {
                var cs = s;
                var ast = s as T;
                if (ast != null && matching(ast))
                    yield return new TokenReference<T>(ast, () => Selectors.Remove(cs), by =>
                    {
                        var i = Selectors.IndexOf(cs);
                        if (i >= 0)
                            Selectors[i] = by as Selector;
                    });
                else
                {
                    foreach (var sub in s.Find(matching))
                        yield return sub;
                }
            }
        }

        public override Selector Clone(Predicate<Selector> filter)
        {
            var s = new SelectorList
            {
                Selectors = Selectors == null ? null : Selectors.Select(x => x.Clone(filter)).Where(x=>x!=null).ToList(),
            };
            if (s.Selectors == null || s.Selectors.Count == 0)
                return null;
            if (s.Selectors != null && s.Selectors.Count == 1)
                return s.Selectors[0];
            s.SetParsingSource(this);
            return s;
        }

        public override bool IsValid
        {
            get { return Selectors.All(x => x.IsValid); }
        }
    }





    // ================================================================================================
    // ========================================= PARSER ===============================================
    // ================================================================================================


    namespace Parsers
    {
        internal class SelectorParser : Parser<Selector>
        {
            internal override Selector DoParse()
            {
                if (End)
                    return null;
                var lst = new SelectorList();

                bool expectingNext = false;
                while (true)
                {
                    bool end = false;
                    switch (CurrentChar)
                    {

                        default:
                            var sel = DoParseOne();
                            if (sel == null)
                            {
                                // that's a warning
                                end = true;
                            }
                            else
                                lst.Selectors.Add(sel);
                            break;
                        case ';':
                        case '{':
                        case '}':
                        case ')':
                            // stoop
                            end = true;
                            break;
                    }

                    if (expectingNext && end)
                    {
                        // we've got a token after a ','
                        if (!End)
                            AddError(ErrorCode.UnexpectedToken, CurrentChar.ToString());
                    }

                    expectingNext = !End && CurrentChar == ',';
                    if (expectingNext)
                        Index++;

                    if (End || !expectingNext)
                        end = true;

                    if (end)
                    {
                        if (lst.Selectors.Count == 0)
                            return null;
                        if (lst.Selectors.Count == 1)
                            return lst.Selectors[0];
                        return lst;
                    }
                }
            }

            Selector DoParseOne()
            {
                if (End)
                    return null;

                // todo : check the difference between   "X::selector" and "X ::selector"  or "X :hover" and "X:hover" or "X [condition]" and "X[condition]" and fix accordingly
                //    => it has been assumed that they are the same... but i think i'm wrong

                var lst = new MultiConditionSelector
                {
                    Conditions = new List<Selector>(),
                };
                ResetWasWhitespace();

                while (true)
                {
                    ChildSelector chainWith = null;
                    bool end = false;
                    var pi = Index;


                    switch (CurrentChar)
                    {
                        // ============ group list ==============
                        // todo selector condtions
                        case '[':
                            lst.Conditions.Add(Parse<AttributeCondition>() ?? (Selector) Parse<InvalidSelector>());
                            break;
                        default:
                            if (WasWhitespace)
                            {
                                // aws a space before... that's still a chain
                                chainWith = Parse<ChildSelector>();
                                if (chainWith == null)
                                    throw new ParsingException("Expecting child selector"); // should never happen
                                break;
                            }
                            Selector sel = Parse<SimpleSelector>();
                            if (sel == null)
                            {
                                if (lst.Conditions.Count == 0)
                                    return null;
                                sel = Parse<InvalidSelector>();
                            }
                            lst.Conditions.Add(sel);
                            break;
                        // ========= separators ================
                        case '>':
                        case '~':
                        case '+':
                            chainWith = Parse<ChildSelector>();
                            if (chainWith == null)
                            {
                                AddError(ErrorCode.ExpectingToken, "child selector");
                                return null;
                            }
                            break;
                        case ',':
                        case ';':
                        case '{':
                        case '}':
                        case ')':
                            // stooop ! (warn if ';')
                            end = true;
                            break;
                    }

                    // safety check
                    if (!end && pi == Index)
                    {
                        AddError(ErrorCode.ExpectingToken, "valid selector");
                        end = true;
                    }

                    if (chainWith != null)
                    {
                        if (lst.Conditions.Count > 0)
                        {
                            if (lst.Conditions.Count == 1)
                                chainWith.Parent = lst.Conditions[0];
                            else
                                chainWith.Parent = lst;
                        }
                        return chainWith;
                    }

                    if (End)
                        end = true;

                    if (end)
                    {
                        if (lst.Conditions.Count == 0)
                            return null;
                        if (lst.Conditions.Count == 1)
                            return lst.Conditions[0];
                        return lst;
                    }
                }
            }
        }
    }
}