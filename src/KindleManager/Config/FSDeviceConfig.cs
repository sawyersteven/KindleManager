using Newtonsoft.Json;

namespace KindleManager.Config
{
    public class FSDeviceConfig : ConfigBase
    {
        #region serializable properties
        private string _LibraryRoot = "";
        [JsonProperty]
        public string LibraryRoot
        {
            get => _LibraryRoot;
            set
            {
                _LibraryRoot = value.TrimEnd(new char[] { '\\', '/' });
            }
        }
        [JsonProperty]
        public string DirectoryFormat { get; set; }
        [JsonProperty]
        public bool ChangeTitleOnSync { get; set; }
        [JsonProperty]
        public string TitleTemplate { get; set; }
        #endregion

        #region method overrides
        public override void SetDefaults()
        {
            LibraryRoot = "documents";
            DirectoryFormat = "{Author}";
            ChangeTitleOnSync = false;
            TitleTemplate = "{Series} {SeriesNum} {Title}";
        }
        #endregion

        public FSDeviceConfig(string filePath)
        {
            base.Open(filePath);
        }

        public void CopyFrom(FSDeviceConfig donor)
        {
            LibraryRoot = donor.LibraryRoot;
            DirectoryFormat = donor.DirectoryFormat;
            ChangeTitleOnSync = donor.ChangeTitleOnSync;
            TitleTemplate = donor.TitleTemplate;
            Write();
        }

        public FSDeviceConfig(FSDeviceConfig c)
        {
            LibraryRoot = c.LibraryRoot;
            DirectoryFormat = c.DirectoryFormat;
            ChangeTitleOnSync = c.ChangeTitleOnSync;
            TitleTemplate = c.TitleTemplate;
        }
    }
}
