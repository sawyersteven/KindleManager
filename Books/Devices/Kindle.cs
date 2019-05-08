using Formats;
using System.IO;
using System.Collections.Generic;

namespace Devices
{
    class Kindle : IDevice
    {

        private readonly string directory;
        public bool firstUse { get; }
        public string configFile { get; }
        public Config config { get; set; }

        public Kindle()
        {
            directory = @"C:\Users\Steven\Desktop\FakeKindle\";
            configFile = Path.Combine(directory, "KindleManager.conf");

            firstUse = !File.Exists(Path.Combine(directory, "KindleManager.conf"));
        }

        public void SendBook(IBook localBook)
        {
            Dictionary<string, string> props = localBook.Props();

            string remoteFile = Path.Combine(directory, config.DirectoryFormat);
            if (config.ChangeTitleOnSync)
            {
                remoteFile = Path.Combine(remoteFile, config.TitleFormat) + ".mobi";
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
