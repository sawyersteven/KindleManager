using System.Diagnostics;

namespace NCss
{
    /// <summary>
    /// Base class for all parsing types
    /// </summary>
    /// <typeparam name="T">Parsed object</typeparam>
    [DebuggerStepThrough]
    abstract class Parser<T> : ParserBase
            where T : CssToken
    {
        protected sealed override object DoParsePrivate()
        {
            return this.DoParse();
        }

        internal abstract T DoParse();
    }
}