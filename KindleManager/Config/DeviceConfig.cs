namespace KindleManager.Config
{
    public class DeviceConfig : IConfig
    {
        public string LibraryRoot { get; set; }
        public string DirectoryFormat { get; set; }
        public bool ChangeTitleOnSync { get; set; }
        public string TitleFormat { get; set; }

        public void SetDefaults()
        {
            LibraryRoot = "documents/";
            DirectoryFormat = "{Author}/";
            ChangeTitleOnSync = false;
            TitleFormat = "{Series} {SeriesNum} {Title}";
        }

        public DeviceConfig() { }

        public DeviceConfig(DeviceConfig c)
        {
            LibraryRoot = c.LibraryRoot;
            DirectoryFormat = c.DirectoryFormat;
            ChangeTitleOnSync = c.ChangeTitleOnSync;
            TitleFormat = c.TitleFormat;
        }
    }
}
