using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace KindleManager
{
    public partial class App : Application
    {
        public static Database Database;
        public string LibraryDirectory;
        public static ConfigManager ConfigManager;

        public void StartApp(object sender, StartupEventArgs e)
        {
            ConfigManager = new ConfigManager();

            LibraryDirectory = Environment.ExpandEnvironmentVariables(ConfigManager.config.LibraryDir);

            Directory.CreateDirectory(LibraryDirectory);

            Database = new Database(Path.Combine(LibraryDirectory, "Library.db"));

            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));

            MainWindow = new MainWindow();
            MainWindow.Show();
        }
    }
}
