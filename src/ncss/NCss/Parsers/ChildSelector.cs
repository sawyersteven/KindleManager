using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable once CheckNamespace
namespace NCss
{
    public class ChildSelector : Selector
    {
        public enum ChildType
        {
            FirstPredecesorOf = (int) '+',
            PredecesorOf = (int) '~',
            DirectChild = (int) '>',
            AnyChild = (int) ' ',
        }

        public ChildType Type { get; set; }

        public Selector Child { get; set; }
        public Selector Parent { get; set; }

        internal override void AppendTo(StringBuilder sb)
        {
            if (Parent != null)
                Parent.AppendTo(sb);
            sb.Append((char) Type);
            if (Child != null)
                Child.AppendTo(sb);
        }


        public override bool IsValid
        {
            get
            {
                if (Parent != null && !Parent.IsValid)
                    return false;
                if (Child != null && !Child.IsValid)
                    return false;
                return Parent != null || Child != null;
            }
        }

        public override IEnumerable<TokenReference<T>> Find<T>(Predicate<T> matching)
        {
            if (Child != null)
            {
                var ast = Child as T;
                if (ast != null && matching(ast))
                    yield return new TokenReference<T>(ast, () => Child = null, by => Child = by as Selector);
                else
                {
                    foreach (var sub in Child.Find(matching))
                        yield return sub;
                }
            }
            if (Parent != null)
            {
                var ast = Parent as T;
                if (ast != null && matching(ast))
                    yield return new TokenReference<T>(ast, () => Parent = null, by => Parent = by as Selector);
                else
                {
                    foreach (var sub in Parent.Find(matching))
                        yield return sub;
                }
            }
        }

        public override Selector Clone(Predicate<Selector> filter)
        {
            if (filter != null && (Child == null || Parent == null))
                return null;
            var s = new ChildSelector
            {
                Child = Child == null ? null : Child.Clone(filter),
                Parent = Parent == null ? null : Parent.Clone(filter),
                Type = Type,
            };
            if (filter != null)
            {
                if (s.Child == null && s.Parent == null)
                    return null;
                s.Child = s.Child ?? Child.Clone(null);
                s.Parent = s.Parent ?? Parent.Clone(null);
            }
            
            s.SetParsingSource(this);
            return s;
        }
    }



    // ================================================================================================
    // ========================================= PARSER ===============================================
    // ================================================================================================



    namespace Parsers
    {

        internal class ChildSelectorParser : Parser<ChildSelector>
        {
            internal override ChildSelector DoParse()
            {
                var isDef = Enum.IsDefined(typeof (ChildSelector.ChildType), (int) CurrentChar);
                if (!isDef && !WasWhitespace)
                    return null;
                var type = isDef ? (ChildSelector.ChildType) CurrentChar : ChildSelector.ChildType.AnyChild;

                if (isDef)
                    Index++; // skip '>'
                else
                    ResetWasWhitespace();

                if (End)
                {
                    AddError(ErrorCode.UnexpectedEnd, "selector");
                    return null;
                }

                var child = Parse<Selector>();
                if (child == null)
                {
                    AddError(ErrorCode.ExpectingToken, "valid child selector");
                    return null;
                }
                return new ChildSelector
                {
                    Type = type,
                    Child = child,
                };
            }

        }
    }
}