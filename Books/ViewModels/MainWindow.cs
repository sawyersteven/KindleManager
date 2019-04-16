using System;
using ReactiveUI;
using System.Reactive;
using System.Windows.Forms;
using System.Linq;
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
            ErrorDialog = ReactiveCommand.Create(_ErrorDialog);
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
        public ReactiveCommand<Unit, Unit> ErrorDialog { get; set; }
        private void _ErrorDialog()
        {
            var err = new Dialogs.Error("Test", "Message");
            err.ShowDialog();
        }

        public ReactiveCommand<Unit, Unit> ImportBook { get; set; }
        private void _ImportBook()
        {
            var dlg = new OpenFileDialog();
            dlg.Multiselect = false;
            dlg.Filter = "eBooks (*.epub;*.mobi)|*.epub;*.mobi";

            if (dlg.ShowDialog() != DialogResult.OK) return;

            Formats.IBook importBook = null;
            try
            {
                switch (Path.GetExtension(dlg.FileName))
                {
                    case ".mobi":
                        importBook = new Formats.Mobi(dlg.FileName);
                        break;
                    case ".epub":
                        importBook = new Formats.Epub(dlg.FileName);
                        break;
                    default:
                        throw new Exception("Unsupported file type");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                MessageBox.Show(e.Message);
                return;
            }

            try
            {
                App.Database.AddBook(importBook);
            }
            catch (InvalidOperationException e)
            {
                MessageBox.Show(e.Message);
                return;
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
            Formats.IBook book = null;

            try
            {
                switch (SelectedTableRow.Format)
                {
                    case "MOBI":
                        book = new Formats.Mobi(SelectedTableRow.FilePath);
                        break;
                    case "EPUB":
                        book = new Formats.Epub(SelectedTableRow.FilePath);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                var err = new Dialogs.Error("Unable to open book for editing", e.Message);
                return;
            }

            if(book == null)
            {
                throw new NotImplementedException($"Book Type {SelectedTableRow.Format} has not been added to the Metadata Editor");
            }

            book.DateAdded = SelectedTableRow.DateAdded;
            book.Id = SelectedTableRow.Id;

            var dlg = new Dialogs.MetadataEditor(book);
            dlg.ShowDialog();
        }


        #endregion

    }
}
