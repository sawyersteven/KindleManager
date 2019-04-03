using System.Windows;
using System.IO;
using System;
using ExtensionMethods;

namespace Books
{
    public partial class App : Application
    {
        public static Database Database;
        readonly string DataDir = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Documents\BookApp\");

        public void StartApp(object sender, StartupEventArgs e)
        {
            Test();
            return;
            Directory.CreateDirectory(DataDir);

            Database = new Database(Path.Combine(DataDir, "Library.db"));

            MainWindow = new MainWindow();
            MainWindow.Show();
        }

        public void Test()
        {
            string path = @"C:\Users\Steven\Downloads\The Last Colony-John Scalzi - Onbekend.epub";

            Formats.Epub book = new Formats.Epub(path);
            book.Print();

            // book.Write();
        }
    }
}
