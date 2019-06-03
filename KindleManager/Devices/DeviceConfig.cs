namespace Devices
{
    public class DeviceConfig
    {
        public string _LibraryRoot = "documents/";
        public string LibraryRoot
        {
            get => _LibraryRoot;
            set
            {
                _LibraryRoot = value;
            }
        }

        public string _DirectoryFormat = "{Author}/{Title}/";
        public string DirectoryFormat
        {
            get => _DirectoryFormat;
            set
            {
                _DirectoryFormat = value;
            }
        }
        public bool _ChangeTitleOnSync = false;
        public bool ChangeTitleOnSync
        {
            get => _ChangeTitleOnSync;
            set
            {
                _ChangeTitleOnSync = value;
            }
        }

        public string _TitleFormat = "{Series} {SeriesNum} {Title}";
        public string TitleFormat
        {
            get => _TitleFormat;
            set
            {
                _TitleFormat = value;
            }
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
