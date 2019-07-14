using System;
using System.Collections.Generic;
using System.Linq;

namespace KindleManager.Config
{
    public class PCConfig : IConfig
    {
        public PCConfig() { }

        public void SetDefaults()
        {
            LibraryRoot = Environment.ExpandEnvironmentVariables(@"%PROGRAMDATA%\KindleManager\Library\");
            DirectoryFormat = "{Author}\\{Series}\\";
            HiddenColumns = new string[] { "Id", "Language", "ISBN", "FilePath", "Contributor", "Subject", "Description", "Rights" };
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        public PCConfig(PCConfig c)
        {
            LibraryRoot = c.LibraryRoot;
            DirectoryFormat = c.DirectoryFormat;
            HiddenColumns = c.HiddenColumns;
        }

        public string LibraryRoot { get; set; }
        public string DirectoryFormat { get; set; }

        private readonly HashSet<string> alwaysHidden = new HashSet<string>() { "Contributor", "Id", "Description", "Rights" };

        private string[] _HiddenColumns;
        public string[] HiddenColumns
        {
            get => _HiddenColumns;
            set
            {
                HashSet<string> hc = new HashSet<string>(alwaysHidden);
                foreach (string i in value)
                {
                    hc.Add(i);
                }
                _HiddenColumns = hc.ToArray();
            }
        }

    }
}
