using Newtonsoft.Json;
using System;
using System.IO;

namespace KindleManager
{
    public class ConfigManager<T>
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string filePath;
        public T config;

        public ConfigManager(string filePath)
        {
            this.filePath = filePath;
            config = (T)Activator.CreateInstance(typeof(T));

            if (!(config is Config.IConfig c)) throw new NotImplementedException();

            if (!File.Exists(filePath))
            {
                Logger.Info("Creating new {} config at {}.", typeof(T), filePath);
                c.SetDefaults();
                Write();
            }
            else
            {
                Logger.Info("Opening {} config from {}", typeof(T), filePath);
                Read();
            }
        }

        public T Copy()
        {
            return (T)Activator.CreateInstance(typeof(T), config);
        }

        /// <summary>
        /// Writes object to filePath as JSON
        /// </summary>
        public void Write()
        {
            File.WriteAllText(filePath, JsonConvert.SerializeObject(config));
        }

        /// <summary>
        /// Sets config to newConfig and writes object to filePath as JSON
        /// </summary>
        public void Write(T newConfig)
        {
            config = newConfig;
            Write();
        }

        /// <summary>
        /// Reads JSON from filePath into object. Will overwrite existing values.
        /// </summary>
        public void Read()
        {
            config = JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
        }
    }
}
