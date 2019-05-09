using System.Windows;
using System.IO;
using System;

namespace Books
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

            MainWindow = new MainWindow();
            MainWindow.Show();
        }


    }
}
