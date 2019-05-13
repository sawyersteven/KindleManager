using Formats;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Devices
{
    class Kindle : IDevice
    {

        public string DriveLetter { get; set; }
        public string Name { get; }
        public string Description { get; }
        public bool FirstUse {
            get
            {
                return !File.Exists(Path.Combine(DriveLetter, "KindleManager.conf"));
            }
        }
        public string ConfigFile { get; }
        public DeviceConfig Config { get; set; }

        public Kindle(string Letter, string Name, string Description)
        {
            DriveLetter = Letter;
            this.Name = Name;
            this.Description = Description;
            ConfigFile = Path.Combine(DriveLetter, "KindleManager.conf");
        }

        public void WriteConfig(DeviceConfig c)
        {
            File.WriteAllText(ConfigFile, JsonConvert.SerializeObject(this));
            Config = c;
        }


        public void SendBook(IBook localBook)
        {
            Dictionary<string, string> props = localBook.Props();

            string remoteFile = Path.Combine(DriveLetter, Config.DirectoryFormat);
            if (Config.ChangeTitleOnSync)
            {
                remoteFile = Path.Combine(remoteFile, Config.TitleFormat) + ".mobi";
            }
            else
            {
                remoteFile = Path.Combine(remoteFile, Path.GetFileName(localBook.FilePath));
            }

            foreach (KeyValuePair<string, string> kv in props)
            {
                remoteFile = remoteFile.Replace($"{{{kv.Key}}}", kv.Value);
            }

            remoteFile = Path.Combine(remoteFile);

            Directory.CreateDirectory(Path.GetDirectoryName(remoteFile));

            File.Copy(localBook.FilePath, remoteFile);
        }
    }
}
