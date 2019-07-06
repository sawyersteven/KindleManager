using NLog;
using NLog.Config;
using System.IO;

namespace KindleManager
{
    static class Logging
    {
        public static void Start(string directory)
        {
            LoggingConfiguration config = new LoggingConfiguration();
            NLog.Targets.FileTarget logFile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = Path.Combine(directory, "log.txt"),
                ArchiveFileName = Path.Combine(directory, "log.{#}.txt"),
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Rolling,
                MaxArchiveFiles = 7,
                ConcurrentWrites = true
            };
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logFile);
#if DEBUG
            NLog.Targets.ConsoleTarget logConsole = new NLog.Targets.ConsoleTarget("logconsole");
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logConsole);
#endif
            LogManager.Configuration = config;
        }
    }
}
