using Newtonsoft.Json;
using System;

namespace KindleManager.Config
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PCConfig : ConfigBase
    {
        #region serialiable properties
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
        public string[] HiddenColumns { get; set; }
        #endregion

        #region method overrides
        public override void SetDefaults()
        {
            LibraryRoot = Environment.ExpandEnvironmentVariables(@"%PROGRAMDATA%\KindleManager\Library");
            DirectoryFormat = "{Author}\\{Series}";
            HiddenColumns = new string[] { "Contributor", "Id", "Description", "Rights", "Language", "ISBN", "FilePath", "Subject" };

        }
        #endregion

        /// <summary>
        /// Opens or creates new config at filePath
        /// </summary>
        /// <param name="filePath"></param>
        public PCConfig(string filePath)
        {
            base.Open(filePath);
        }

        /// <summary>
        /// Copies object props without any disk operations
        /// </summary>
        /// <param name="c"></param>
        public PCConfig(PCConfig c)
        {
            filePath = c.filePath;
            LibraryRoot = c.LibraryRoot;
            DirectoryFormat = c.DirectoryFormat;
            HiddenColumns = c.HiddenColumns;
        }
    }
}
