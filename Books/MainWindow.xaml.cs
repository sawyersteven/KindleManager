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

            LibraryTable.DragEnter += Library_DragEnter;
            LibraryTable.DragLeave += Library_DragLeave;
            LibraryTable.Drop += Library_Drop;

            foreach (var c in LibraryTable.Columns)
            {
                if (App.ConfigManager.config.HiddenColumns.Contains(c.Header))
                {
                    c.Visibility = Visibility.Collapsed;
                }
            }
        }
               
        #region Library Drag/Drop

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
        #endregion

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

        private class Sorter : IComparer
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

        #region ContextMenu Helpers

        private void OpenContextMenu(object sender, RoutedEventArgs e)
        {
            ((Control)sender).ContextMenu.IsOpen = true;
        }

        /* Because ContextMenu controls can't use the main window's datacontext
         * and therefore cannot bind directly to methods in the dc, this is a
         * workaround. These methods just call their equivalent in the dc.
         */

        private void SendBook(object sender, RoutedEventArgs e)
        {
            ((ViewModels.MainWindow)DataContext)._SendBook(LibraryTable.SelectedItems);
        }

        private void ReceiveBook(object sender, RoutedEventArgs e)
        {
            ((ViewModels.MainWindow)DataContext)._ReceiveBook();
        }

        private void EditMetadata(object sender, RoutedEventArgs e)
        {
            ((ViewModels.MainWindow)DataContext)._EditMetadata();
        }
        
        private void OpenBookFolder(object sender, RoutedEventArgs e)
        {
            ((ViewModels.MainWindow)DataContext)._OpenBookFolder();
        }
        private void RemoveBook(object sender, RoutedEventArgs e)
        {
            ((ViewModels.MainWindow)DataContext)._RemoveBook();
        }
        #endregion
    }
}
