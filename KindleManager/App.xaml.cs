using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace KindleManager
{
    public partial class App : Application
    {
        public static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static string LibraryDirectory;
        public static Config.PCConfig Config;
        public static Devices.LocalLibrary LocalLibrary;

        public void StartApp(object sender, StartupEventArgs e)
        {
            string appDataDir = Environment.ExpandEnvironmentVariables(@"%PROGRAMDATA%\KindleManager\");

            Logging.Start(appDataDir);

            Config = new Config.PCConfig(Path.Combine(appDataDir, "Settings.conf"));

            LibraryDirectory = Environment.ExpandEnvironmentVariables(Config.LibraryRoot);
            Directory.CreateDirectory(LibraryDirectory);

            LocalLibrary = new Devices.LocalLibrary(Path.Combine(appDataDir, "Library.db"));

            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));

            MainWindow = new MainWindow();
            MainWindow.Show();
        }
    }
}
