using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// ReSharper disable once CheckNamespace
namespace NCss
{
    public class Stylesheet : CssToken
    {
        public List<Rule> Rules { get; set; } = new List<Rule>();

        public override bool IsValid
        {
            get { return Rules.All(x => x.IsValid); }
        }

        internal override void AppendTo(StringBuilder sb)
        {
            AppendToWithOptions(sb, CssRestitution.OnlyWhatYouUnderstood);
        }

        public override void AppendToWithOptions(StringBuilder sb, CssRestitution option)
        {
            if (Rules == null)
                return;
            foreach (var r in Rules.Where(x => x != null))
                r.AppendToWithOptions(sb, option);
        }

        public IEnumerable<TokenReference<T>> Find<T>(Predicate<T> matching, Search where = Search.All)
            where T:CssToken
        {
            if(Rules == null)
                yield break;

            foreach (var r in Rules)
            {
                var rc = r;
                var asT = r as T;
                if (asT != null && matching(asT))
                    yield return new TokenReference<T>(asT, () => Rules.Remove(rc), by =>
                    {
                        var i = Rules.IndexOf(rc);
                        if (i >= 0)
                            Rules[i] = by as Rule;
                    });
                else
                {
                    foreach (var sub in r.Find(matching, where))
                    {
                        yield return sub;
                    }
                }
            }
        }

        public Stylesheet Clone<T>(Predicate<T> filter) where T : Selector
        {
            return Clone(x =>
            {
                var ast = x as T;
                return ast != null && filter(ast);
            });
        }
        
        public Stylesheet Clone(Predicate<Selector> filter=null)
        {
            var sh = new Stylesheet
            {
                Rules = Rules == null ? null : Rules.Select(x => x.Clone(filter)).Where(x=>x!=null).ToList(),
            };
            sh.SetParsingSource(this);
            return sh;
        }
    }


    public class TokenReference<T>
            where T : CssToken
    {
        public T Token { get; private set; }
        public Action Remove { get; private set; }
        public Action<T> ReplaceBy { get; private set; }

        internal TokenReference(T token, Action remove, Action<T> replaceBy)
        {
            Token = token;
            Remove = remove;
            ReplaceBy = replaceBy;
        }
    }
    [Flags]
    public enum Search
    {
        InSelectors = 1,
        InProperties = 1<<1,
        InPropertyValues = InProperties | 1<<2,
        All = InSelectors | InPropertyValues,
    }




    // ================================================================================================
    // ========================================= PARSER ===============================================
    // ================================================================================================


    namespace Parsers
    {
        internal class StylesheetParser : Parser<Stylesheet>
        {
            internal override Stylesheet DoParse()
            {
                var sh = new Stylesheet();
                while (!End)
                {
                    var ind = Index;
                    // supposted to be executed only once... But if some BlockMismatch exception happens, that's not the case.
                    // ... And we dont want to stop parsing on some crappy scenario (even if that's invalid CSS)
                    sh.Rules.AddRange(ParseBlock<Rule>(false));
                    if (!End && Index == ind)
                    {
                        throw new ParsingException("Failed to parse to the end of CSS");
                    }
                }
                return sh;
            }
        }
    }
}
