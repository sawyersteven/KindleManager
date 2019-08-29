namespace NCss
{
    public class CssParsingError
    {
        public ErrorCode Code { get; private set; }
        public string Details { get; private set; }

        public string AroundView { get; private set; }

        public int At { get; private set; }

        internal CssParsingError(ErrorCode code, string details, int at, string around)
        {
            Code = code;
            Details = details;
            At = at;
            AroundView = around;
        }

        public override string ToString()
        {
            return Code + ": " + Details + ", around " + AroundView;
        }
    }
}