using MahApps.Metro.Controls;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.ComponentModel;
using System.Collections;

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

            //LibraryTable.ItemsSource = App.Database.Library;
            LibraryTable.DragEnter += Library_DragEnter;
            LibraryTable.DragLeave += Library_DragLeave;
            LibraryTable.Drop += Library_Drop;
        }

        private void OpenDeviceExtraMenu(object sender, RoutedEventArgs e)
        {
            DeviceContextMenu.IsOpen = true;
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

        /// <summary>
        /// Custom sort logic for InLibrary columns
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortLibraryTable(object sender, System.Windows.Controls.DataGridSortingEventArgs e)
        {
            DataGridColumn column = e.Column;

            if (column.DisplayIndex == 0 || column.DisplayIndex == 1)
            {
                e.Handled = true;
                ViewModels.MainWindow vm = (ViewModels.MainWindow)DataContext;

                ObservableCollection<Database.BookEntry> library = (column.DisplayIndex == 0) ? vm.LocalLibrary : vm.RemoteLibrary;

                // column.SortDirection can be null, so default to Ascending
                ListSortDirection direction = (column.SortDirection != ListSortDirection.Ascending) ? ListSortDirection.Ascending : ListSortDirection.Descending;
                column.SortDirection = direction;

                ListCollectionView books = (ListCollectionView)CollectionViewSource.GetDefaultView(((DataGrid)sender).ItemsSource);

                //apply the sort
                books.CustomSort = new Sorter(library, direction);
            }

        }

        private struct Sorter : IComparer
        {
            private ListSortDirection direction;
            private ObservableCollection<Database.BookEntry> library;

            public Sorter(ObservableCollection<Database.BookEntry> l, ListSortDirection d)
            {
                direction = d;
                library = l;
            }

            public int Compare(object x, object y)
            {
                int a = library.Any(z => z.Id == ((Database.BookEntry)x).Id) ? 1 : 0;
                int b = library.Any(z => z.Id == ((Database.BookEntry)y).Id) ? 1 : 0;
                return (direction == ListSortDirection.Ascending) ? a - b : b - a;
            }
        }
    }
}
