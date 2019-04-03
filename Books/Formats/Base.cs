namespace Formats
{
    public interface IBook
    {
        int Id { get; set; }
        string FilePath { get; set; }
        string Type { get; }
        string Title { get; set; }
        string Author { get; set; }
        string Series { get; set; }
        float SeriesNum { get; set; }
        string Publisher { get; set; }
        string PubDate { get; set; }
        ulong ISBN { get; set; }
        string DateAdded { get; set; }

        void Write();
    }
}
