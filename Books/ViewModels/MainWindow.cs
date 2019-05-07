using System;
using ReactiveUI;
using System.Reactive;
using System.Windows.Forms;
using Formats;
using System.Collections.ObjectModel;
using System.IO;

namespace Books.ViewModels
{
    class MainWindow : ReactiveObject
    {

        public MainWindow()
        {
            ImportBook = ReactiveCommand.Create(_ImportBook);
            RemoveBook = ReactiveCommand.Create(_RemoveBook);
            EditMetadata = ReactiveCommand.Create(_EditMetadata);
            OpenKindle = ReactiveCommand.Create(_OpenKindle);
        }

        #region properties
        public ObservableCollection<Database.BookEntry> Library { get; } = App.Database.Library;

        private Database.BookEntry _SelectedTableRow;
        public Database.BookEntry SelectedTableRow
        {
            get => _SelectedTableRow;
            set
            {
                this.RaiseAndSetIfChanged(ref _SelectedTableRow, value);
            }
        }
        #endregion

        #region button commands
        public ReactiveCommand<Unit, Unit> OpenKindle { get; set; }
        private void _OpenKindle()
        {
        }

        public ReactiveCommand<Unit, Unit> ImportBook { get; set; }
        private void _ImportBook()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = false;
            dlg.Filter = "eBooks (*.epub;*.mobi)|*.epub;*.mobi";

            if (dlg.ShowDialog() != DialogResult.OK) return;

            IBook importBook;
            try
            {
                importBook = Converters.NewIBook(dlg.FileName);
            }
            catch (Exception e)
            {
                var errdlg = new Dialogs.Error("Error opening book", e.Message);
                errdlg.ShowDialog();
                return;
            }

            string authorDir = Path.Combine(App.DataDir, importBook.Author);
            Directory.CreateDirectory(authorDir);

            // Todo make filepath from config eg "{Author}/{Series}/{Title}.mobi"
            string destinationFile = Path.Combine(authorDir, importBook.Title) + ".mobi";

            if (Path.GetExtension(dlg.FileName) != ".mobi")
            {    
                var convertdlg = new Dialogs.ConvertRequired(Path.GetFileName(dlg.FileName));
                if (convertdlg.ShowDialog() == false) return;
                try
                {
                    importBook = Converters.ToMobi(importBook, destinationFile);
                }
                catch (InvalidOperationException e)
                {
                    MessageBox.Show(e.Message);
                    return;
                }
            }
            else
            {
                try
                {
                    File.Copy(importBook.FilePath,destinationFile);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    return;
                }
            }

            try
            {
                App.Database.AddBook(importBook);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return;
            }
        }

        public ReactiveCommand<Unit, Unit> RemoveBook { get; set; }
        private void _RemoveBook()
        {
            if (SelectedTableRow == null) { return; }

            var dlg = new Dialogs.ConfirmRemoveFile(SelectedTableRow.Title);

            if (dlg.ShowDialog() == false) { return; }

            try
            {
                App.Database.RemoveBook(SelectedTableRow);
            }
            catch (Exception e)
            {
                var err = new Dialogs.Error("Unable to save metadata", e.Message);
                err.ShowDialog();
            }
            return;
        }

        public ReactiveCommand<Unit, Unit> EditMetadata { get; set; }
        private void _EditMetadata()
        {
            if (SelectedTableRow == null) return;
            IBook book = null;
            try
            {
                book = new Formats.Mobi.Book(SelectedTableRow.FilePath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                var err = new Dialogs.Error("Unable to open book for editing", e.Message);
                return;
            }

            book.DateAdded = SelectedTableRow.DateAdded;
            book.Id = SelectedTableRow.Id;

            var dlg = new Dialogs.MetadataEditor(book);
            dlg.ShowDialog();
        }
        #endregion
    }
}
