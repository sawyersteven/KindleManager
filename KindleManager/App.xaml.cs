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
        public static ConfigManager ConfigManager;
        public static Library LocalLibrary;

        public void StartApp(object sender, StartupEventArgs e)
        {
            ConfigManager = new ConfigManager();

            LibraryDirectory = Environment.ExpandEnvironmentVariables(ConfigManager.config.LibraryDir);

            Directory.CreateDirectory(LibraryDirectory);

            LocalLibrary = new Library();

            Database = new Database(Path.Combine(LibraryDirectory, "Library.db"));

            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));

            Logging.Start(Path.Combine(Environment.ExpandEnvironmentVariables(@"%PROGRAMDATA%\KindleManager\")));

            MainWindow = new MainWindow();
            MainWindow.Show();
        }
    }
}
