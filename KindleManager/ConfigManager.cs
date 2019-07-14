using ExtensionMethods;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace KindleManager
{
    public class ConfigManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string path = Path.Combine(Environment.ExpandEnvironmentVariables(@"%PROGRAMDATA%\KindleManager\"), "Settings.conf");

        public Config config;

        public ConfigManager()
        {
            config = new Config();
            if (!File.Exists(path))
            {
                Logger.Info("Creating new config at {}.", path);
                config.SetDefaults();
                File.WriteAllText(path, JsonConvert.SerializeObject(config));
            }
            else
            {
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
            }
        }

        public void Write()
        {
            Logger.Info("Writing config to {}.", path);
            File.WriteAllText(path, JsonConvert.SerializeObject(config));
        }

        public class Config
        {
            public Config() { }

            public void SetDefaults()
            {
                LibraryDir = Environment.ExpandEnvironmentVariables(@"%PROGRAMDATA%\KindleManager\Library\");
                LibraryFormat = "{Author}\\{Series}\\";
                HiddenColumns = new string[] { "Id", "Language", "ISBN", "FilePath", "Contributor", "Subject", "Description", "Rights" };
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            public Config(Config c)
            {
                LibraryDir = c.LibraryDir;
                LibraryFormat = c.LibraryFormat;
                HiddenColumns = c.HiddenColumns;
            }

            public string LibraryDir { get; set; }
            public string LibraryFormat { get; set; }

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
}
