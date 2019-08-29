using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// ReSharper disable once CheckNamespace
namespace NCss
{
    public abstract class Rule : CssBlockToken
    {
        internal Rule() { }

        public List<Property> Properties { get; set; } = new List<Property>();

        internal abstract BodyType ExpectedBodyType { get; }
        public List<Rule> ChildRules { get; set; } = new List<Rule>();

        internal enum BodyType
        {
            None,
            Properties,
            ChildRules,
        }

        protected virtual IEnumerable<TokenReference<T>> InnerFind<T>(Predicate<T> matching)
            where T : CssToken
        {
            yield break;
        }

        public IEnumerable<TokenReference<T>> Find<T>(Predicate<T> matching, Search where)
            where T:CssToken
        {
            if (Properties != null && where.HasFlag(Search.InProperties))
            {
                foreach (Property p in Properties)
                {
                        var asT = p as T;
                    if (asT != null && matching(asT))
                        yield return new TokenReference<T>(asT, () => Properties.Remove(p), other =>
                        {
                            var i = Properties.IndexOf(p);
                            if (i >= 0)
                                Properties[i] = other as Property;
                        });
                    else if (where.HasFlag(Search.InPropertyValues))
                    {
                        foreach (var subv in p.Find<T>(matching))
                            yield return subv;
                    }
                }
            }

            if (ChildRules != null)
            {
                foreach (var match in ChildRules.SelectMany(p => p.Find<T>(matching,where)))
                {
                    yield return match;
                }
            }

            if (where.HasFlag(Search.InSelectors))
            {
                foreach (var match in InnerFind<T>(matching))
                {
                    yield return match;
                }
            }

        }

        public abstract Rule Clone(Predicate<Selector> filter);
    }

    internal class OrphanBlockRule : Rule
    {
        internal override void AppendTo(StringBuilder sb)
        {
        }
        
        public override bool IsValid
        {
            get { return false; }
        }

        internal override BodyType ExpectedBodyType
        {
            get { return BodyType.Properties; }
        }

        public override Rule Clone(Predicate<Selector> filter)
        {
            if (filter != null)
                return null;
            return this; // i'm immutable :)
        }
    }

    public class ClassRule : Rule
    {
        public override void AppendToWithOptions(StringBuilder sb, CssRestitution option)
        {
            if (Selector != null)
                Selector.AppendToWithOptions(sb, option);
            sb.Append('{');
            if (Properties != null)
            {
                foreach (var p in Properties)
                    p.AppendToWithOptions(sb, option);
            }
            sb.Append('}');
        }


        internal override void AppendTo(StringBuilder sb)
        {
            AppendToWithOptions(sb, CssRestitution.OnlyWhatYouUnderstood);
        }

        public override bool IsValid
        {
            get
            {
                if (Selector == null)
                    return false;
                if (!Selector.IsValid)
                    return false;
                if (Properties == null || Properties.Count == 0)
                    return true;
                var ret = Properties.All(p => p.IsValid);
                return ret;
            }
        }

        public Selector Selector { get; set; }

        internal override BodyType ExpectedBodyType
        {
            get { return BodyType.Properties; }
        }

        protected override IEnumerable<TokenReference<T>> InnerFind<T>(Predicate<T> matching)
        {
            var asT = Selector as T;
            if (asT != null && matching(asT))
            {
                yield return new TokenReference<T>(asT, () => Selector = null, by => Selector = by as Selector);
            }
            else
            {
                foreach (var m in Selector.Find<T>(matching))
                    yield return m;
            }
        }

        public override Rule Clone(Predicate<Selector> filter)
        {
            var r = new ClassRule
            {
                Selector = Selector == null ? null : Selector.Clone(filter),
                Properties = Properties == null ? null : Properties.Select(x=>x.Clone()).ToList(),
            };
            if (filter != null && r.Selector == null)
                return null;
            r.SetParsingSource(this);
            return r;
        }
    }

    public class DirectiveRule : Rule
    {

        internal override void AppendTo(StringBuilder sb)
        {
            if (Selector != null)
                Selector.AppendTo(sb);
            switch (ExpectedBodyType)
            {
                case BodyType.None:
                    sb.Append(';');
                    break;
                case BodyType.Properties:
                    sb.Append('{');
                    if (Properties != null)
                    {
                        foreach (var p in Properties)
                            p.AppendTo(sb);
                    }
                    sb.Append('}');
                    break;
                case BodyType.ChildRules:
                    sb.Append('{');
                    if (ChildRules != null)
                    {
                        foreach (var p in ChildRules)
                            p.AppendTo(sb);
                    }
                    sb.Append('}');
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool IsValid
        {
            get
            {
                if (Selector == null)
                    return false;
                if (!Selector.IsValid)
                    return false;
                switch (ExpectedBodyType)
                {
                    case BodyType.None:
                        return true;
                    case BodyType.Properties:
                        if (Properties == null || Properties.Count == 0)
                            return true;
                        return Properties.All(p => p.IsValid);
                    case BodyType.ChildRules:
                        if (ChildRules == null || ChildRules.Count == 0)
                            return true;
                        return ChildRules.All(x => x.IsValid);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public DirectiveSelector Selector { get; set; }

        internal override BodyType ExpectedBodyType
        {
            get { return Selector == null ? BodyType.None : Selector.ExpectedBodyType; }
        }

        protected override IEnumerable<TokenReference<T>> InnerFind<T>(Predicate<T> matching)
        {
            if(Selector == null || typeof(T) != typeof(DirectiveSelector))
                yield break;
            var st = Selector as T;
            if (matching(st))
                yield return new TokenReference<T>(Selector as T, () => Selector = null, by => Selector = by as DirectiveSelector);

        }

        public override Rule Clone(Predicate<Selector> filter)
        {
            var r = new DirectiveRule
            {
                ChildRules = ChildRules == null ? null : ChildRules.Select(x => x.Clone(filter)).Where(x=>x!=null).ToList(),
                Selector = Selector == null ? null : (DirectiveSelector)Selector.Clone(ExpectedBodyType == BodyType.Properties?filter:null),
                Properties = Properties == null ? null : Properties.Select(x => x.Clone()).ToList(),
            };
            if (filter != null && (r.ChildRules == null || r.ChildRules.Count == 0 || Selector ==null))
                return null;
            r.SetParsingSource(this);
            return r;
        }
    }
    public class NotParsableBlockRule : Rule
    {

        internal override void AppendTo(StringBuilder sb)
        {
            // do nothing
        }

        public override bool IsValid
        {
            get { return false; }
        }

        internal override BodyType ExpectedBodyType
        {
            get { return BodyType.Properties; }
        }

        public override Rule Clone(Predicate<Selector> filter)
        {
            if (filter != null)
                return null;
            return this; // i'm immutable :)
        }
    }
}