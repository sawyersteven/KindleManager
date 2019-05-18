using MahApps.Metro.Controls;
using System.Windows.Controls;
using System.Windows;
using System.Linq;

namespace Books
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            this.DataContext = new ViewModels.MainWindow();
            InitializeComponent();

            LibraryTable.ItemsSource = Library.Books;
            LibraryTable.DragEnter += Library_DragEnter;
            LibraryTable.DragLeave += Library_DragLeave;
            LibraryTable.Drop += Library_Drop;
        }

        private void OpenDeviceExtraMenu(object sender, RoutedEventArgs e)
        {
            ((Button)sender).ContextMenu.IsOpen = true;
        }

        private void Library_Drop(object sender, DragEventArgs e)
        {
            LibraryTable.Opacity = 1.0;
            Cursor = System.Windows.Input.Cursors.Arrow;

            string[] fileList = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (!VerifyDrop(fileList)) return;

            ViewModels.MainWindow vm = (ViewModels.MainWindow)DataContext;
            vm.ImportBooksDrop(fileList[0]);
        }

        private void Library_DragEnter(object sender, DragEventArgs e)
        {
            LibraryTable.Opacity = 0.5;
            string[] fileList = e.Data.GetData(DataFormats.FileDrop) as string[];

            if (!VerifyDrop(fileList))
            {
                Cursor = System.Windows.Input.Cursors.No;
            }
        }

        private void Library_DragLeave(object sender, DragEventArgs e)
        {
            LibraryTable.Opacity = 1.0;
            Cursor = System.Windows.Input.Cursors.Arrow;
        }

        private bool VerifyDrop(string[] paths)
        {
            // Only allow single drops for now;
            if (paths.Length > 1) return false;
            if (System.IO.Directory.Exists(paths[0])) return true;
            return Formats.Resources.CompatibleFileTypes.Contains(System.IO.Path.GetExtension(paths[0]));
        }
    }
}
