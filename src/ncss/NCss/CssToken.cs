using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NCss
{
    public abstract class CssBlockToken : CssToken
    {
        protected internal CssBlockToken()
        {
        }
    }


    /// <summary>
    /// Base class for all CSS items
    /// </summary>
    public abstract class CssToken
    {
        protected internal CssToken()
        {
        }

        string globalCss;
        int fromIndex;
        int toIndex;

        public int FromIndex { get { return fromIndex; } }

        internal void SetParsingSource(CssToken sameAs)
        {
            this.globalCss = sameAs.globalCss;
            this.fromIndex = sameAs.fromIndex;
            this.toIndex = sameAs.toIndex;
            this.Errors = sameAs.Errors==null?null: new List<CssParsingError>(sameAs.Errors);
        }

        [DebuggerStepThrough]
        internal void SetParsingSource(string globalCss, int fromIndex, int toIndex, List<CssParsingError> errors)
        {
            this.globalCss = globalCss;
            this.fromIndex = fromIndex;
            this.toIndex = toIndex;
            Errors = errors;
        }

        /// <summary>
        /// If parsed from raw CSS string, this returns the original string from which this item has been parsed.
        /// </summary>
        public string OriginalToken
        {
            get
            {
                if (globalCss == null)
                    return null;
                return globalCss.Substring(fromIndex, toIndex - fromIndex);
            }
        }
        
        public List<CssParsingError> Errors { get; private set; }
        public bool HasError { get { return Errors != null && Errors.Count > 0; } }

        internal abstract void AppendTo(StringBuilder sb);
        public abstract bool IsValid { get; }

        public virtual void AppendToWithOptions(StringBuilder sb, CssRestitution options)
        {
            if (options.HasFlag(CssRestitution.RemoveErrors) && HasError
                || options.HasFlag(CssRestitution.RemoveInvalid) && !IsValid)
                return;
            if (options.HasFlag(CssRestitution.OriginalWhenErrorOrInvalid) && (HasError || !IsValid))
            {
                sb.Append(OriginalToken);
                return;
            }

            AppendTo(sb);
        }

        public string ToString(CssRestitution options)
        {
            var sb = new StringBuilder();
            AppendToWithOptions(sb, options);
            return sb.ToString();
        }

        public override string ToString()
        {
            return ToString(CssRestitution.OnlyWhatYouUnderstood);
        }
    }

    [Flags]
    public enum CssRestitution
    {
        OnlyWhatYouUnderstood = 0,
        RemoveErrors = 1,
        RemoveInvalid = RemoveErrors | 1<<1,
        OriginalWhenErrorOrInvalid = 1<<2,
    }
}