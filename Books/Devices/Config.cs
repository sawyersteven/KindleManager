using Newtonsoft.Json;
using System.IO;

namespace Devices
{
    public class Config
    {
        public string LibraryRoot { get; set; }
        public string DirectoryFormat { get; set; }
        public bool ChangeTitleOnSync { get; set; }
        public string TitleFormat { get; set; }

        public Config() { }

        public void Write(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this));
        }

    }
}
