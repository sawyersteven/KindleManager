using System.Windows;
using System.IO;
using System;


namespace Books
{
    public partial class App : Application
    {
        public static Database Database;
        readonly string DataDir = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Documents\BookApp\");

        public void StartApp(object sender, StartupEventArgs e)
        {
            //Test.DumpTextHtml();
            //return;
            //Debug.Open();
            Debug.EpubToMobi();

            Console.WriteLine("END");
            return;
            Directory.CreateDirectory(DataDir);

            Database = new Database(Path.Combine(DataDir, "Library.db"));

            MainWindow = new MainWindow();
            MainWindow.Show();
        }


    }
}
