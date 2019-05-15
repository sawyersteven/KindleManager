using System.IO;
using Newtonsoft.Json;

namespace Devices
{
    public abstract class Device
    {

        public virtual string DriveLetter { get; set; }
        public virtual string Name { get; set; }
        public virtual string Description { get; set; }
        public virtual string ConfigFile { get; set; }
        public virtual string DatabaseFile { get; set; }
        public DeviceConfig Config { get; set; }
        public virtual bool FirstUse
        {
            get
            {
                return !File.Exists(Path.Combine(DriveLetter, "KindleManager.conf"));
            }
        }
        public virtual Books.Database Database { get; set; }

        public void WriteConfig(DeviceConfig c)
        {
            File.WriteAllText(ConfigFile, JsonConvert.SerializeObject(c));
            Config = c;
        }

        public void LoadDatabase()
        {
            Database = new Books.Database(DatabaseFile);
        }

        public void Init()
        {
            if (File.Exists(ConfigFile))
            {
                Config = JsonConvert.DeserializeObject<DeviceConfig>(File.ReadAllText(ConfigFile));
            }
            else
            {
                Config = new DeviceConfig();
                WriteConfig(Config);
            }

            LoadDatabase();
        }

        public abstract void SendBook(Formats.BookBase localbook);
    }
}
