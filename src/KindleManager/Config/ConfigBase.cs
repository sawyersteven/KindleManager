using Newtonsoft.Json;
using System.IO;

namespace KindleManager.Config
{
    public abstract class ConfigBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        protected string filePath;

        public abstract void SetDefaults();

        public void Open(string filePath)
        {
            this.filePath = filePath;

            if (!File.Exists(filePath))
            {
                Logger.Info("Creating new config at {}.", filePath);
                SetDefaults();
                Write();
            }
            else
            {
                Logger.Info("Opening config from {}", filePath);
                Read();
            }
        }

        /// <summary>
        /// Writes object to filePath as JSON
        /// </summary>
        public void Write()
        {
            File.WriteAllText(filePath, JsonConvert.SerializeObject(this));
        }

        /// <summary>
        /// Reads JSON from filePath into object. Will overwrite existing values.
        /// </summary>
        public void Read()
        {
            JsonConvert.PopulateObject(File.ReadAllText(filePath), this);
        }
    }
}
