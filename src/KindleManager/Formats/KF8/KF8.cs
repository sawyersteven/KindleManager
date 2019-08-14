namespace Formats.KF8
{
    /// <summary>
    /// This format still remains a bit of an enigma. Officially it is
    /// basically a typical mobi with epub html included in certain
    /// records. But epub->azw3 converters don't include the epub
    /// section and readers don't really seem to mind.
    /// 
    /// Reading a FK8/azw3 is the same as reading a mobi since we
    /// can also completely ignore the epub records - if they are
    /// even present.
    /// </summary>
    class Book : Mobi.Book
    {
        public Book(string filepath) : base(filepath)
        {

        }
    }
}
