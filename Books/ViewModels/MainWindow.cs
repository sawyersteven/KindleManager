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
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = false;
            dlg.Filter = "eBooks (*.epub;*.mobi)|*.epub;*.mobi";

            if (dlg.ShowDialog() != DialogResult.OK) return;

            IBook importBook = null;
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

            if (Path.GetExtension(dlg.FileName) != ".mobi")
            {
                string baseName = Path.GetFileName(dlg.FileName);
                var convertdlg = new Dialogs.ConvertRequired(baseName);
                if (convertdlg.ShowDialog() == false) return;
                try
                {
                    importBook = Converters.ToMobi(importBook);
                }
                catch (InvalidOperationException e)
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
