namespace Formats
{
    public interface IBook
    {
        string FilePath { get; set; }
        string Format { get; }

        string Title { get; set; }
        string Language { get; set; }
        ulong ISBN { get; set; }

        string Author { get; set; }
        string Contributor { get; set; }
        string Publisher { get; set; }
        string[] Subject { get; set; }
        string Description { get; set; }
        string PubDate { get; set; }
        string Rights { get; set; }

        // Not a part of in-file metadata, only stored in local DB
        int Id { get; set; }
        string Series { get; set; }
        float SeriesNum { get; set; }
        string DateAdded { get; set; }

        #region methods
        string TextContent();

        void WriteMetadata(); // Everything except text, images, css, etc.
        void WriteContent(string title, byte[][] images);
        #endregion
    }
}
