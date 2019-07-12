using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace KindleManager
{
    public partial class App : Application
    {
        public static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static Database Database;
        public static string LibraryDirectory;
        public static ConfigManager<Config.PCConfig> ConfigManager;

        public void StartApp(object sender, StartupEventArgs e)
        {
            ConfigManager = new ConfigManager<Config.PCConfig>(Path.Combine(Environment.ExpandEnvironmentVariables(@"%PROGRAMDATA%\KindleManager\"), "Settings.conf"));

            LibraryDirectory = Environment.ExpandEnvironmentVariables(ConfigManager.config.LibraryRoot);

            Directory.CreateDirectory(LibraryDirectory);

            Database = new Database(Path.Combine(LibraryDirectory, "Library.db"));

            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));

            Logging.Start(Path.Combine(Environment.ExpandEnvironmentVariables(@"%PROGRAMDATA%\KindleManager\")));

            MainWindow = new MainWindow();
            MainWindow.Show();
        }
    }
}
