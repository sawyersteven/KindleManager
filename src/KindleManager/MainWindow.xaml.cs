using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace KindleManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow(bool showSplash)
        {
            this.DataContext = new ViewModels.MainWindow();
            InitializeComponent();

            LibraryTable.DragEnter += Library_DragEnter;
            LibraryTable.DragLeave += Library_DragLeave;
            LibraryTable.Drop += Library_Drop;

            foreach (var c in LibraryTable.Columns)
            {
                if (App.Config.HiddenColumns.Contains(c.Header))
                {
                    c.Visibility = Visibility.Collapsed;
                }
            }

            if (showSplash)
            {
                this.ContentRendered += (object sender, System.EventArgs e) => this.GetDataContext()._ShowAbout();
            }
        }

        private ViewModels.MainWindow GetDataContext()
        {
            return (ViewModels.MainWindow)DataContext;
        }

        #region Library Drag/Drop

        private void Library_Drop(object sender, DragEventArgs e)
        {
            LibraryTable.Opacity = 1.0;
            Cursor = System.Windows.Input.Cursors.Arrow;

            string[] fileList = e.Data.GetData(DataFormats.FileDrop) as string[];

            ViewModels.MainWindow vm = (ViewModels.MainWindow)DataContext;
            vm.ImportBooksDrop(fileList);
        }

        private void Library_DragEnter(object sender, DragEventArgs e)
        {
            LibraryTable.Opacity = 0.5;
            string[] fileList = e.Data.GetData(DataFormats.FileDrop) as string[];

            if (!VerifyDrop(fileList))
            {
                Cursor = System.Windows.Input.Cursors.No;
                LibraryTable.AllowDrop = false;
            }
        }

        private void Library_DragLeave(object sender, DragEventArgs e)
        {
            LibraryTable.Opacity = 1.0;
            Cursor = System.Windows.Input.Cursors.Arrow;
            LibraryTable.AllowDrop = true;
        }

        private bool VerifyDrop(string[] paths)
        {
            if (paths.Length > 1) return true;
            return System.IO.Directory.Exists(paths[0]) || Formats.Resources.AcceptedFileTypes.Contains(System.IO.Path.GetExtension(paths[0]));
        }
        #endregion

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
            GetDataContext()._SendBook(LibraryTable.SelectedItems);
        }

        private void ReceiveBook(object sender, RoutedEventArgs e)
        {
            GetDataContext()._ReceiveBook(LibraryTable.SelectedItems);
        }

        private void EditMetadata(object sender, RoutedEventArgs e)
        {
            GetDataContext()._EditMetadata();
        }

        private void OpenBookFolder(object sender, RoutedEventArgs e)
        {
            GetDataContext()._OpenBookFolder();
        }

        private void RemoveBook(object sender, RoutedEventArgs e)
        {
            GetDataContext()._RemoveBook();
        }

        private void SaveLibraryColumns(object sender, RoutedEventArgs e)
        {
            if (!(sender is ContextMenu menu)) return;
            if (!(menu.DataContext is DataGrid grid)) return;
            if (grid.Columns == null) return;

            List<string> hiddenColumns = new List<string>();
            foreach (var a in grid.Columns)
            {
                if (a.Header == null || a.Visibility == Visibility.Visible) continue;
                hiddenColumns.Add((string)a.Header);
            }

            GetDataContext().SaveLibraryColumns(hiddenColumns);
        }
        #endregion

    }
}
