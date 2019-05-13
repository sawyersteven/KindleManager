using System;
using ReactiveUI;
using System.Reactive;
using System.Windows.Forms;
using Formats;
using System.Collections.ObjectModel;
using System.IO;
using Devices;
using Newtonsoft.Json;

namespace Books.ViewModels
{
    class MainWindow : ReactiveObject
    {
        public MainWindow()
        {
            ImportBook = ReactiveCommand.Create(_ImportBook);
            RemoveBook = ReactiveCommand.Create(_RemoveBook);
            EditMetadata = ReactiveCommand.Create(_EditMetadata);
            SyncDevice = ReactiveCommand.Create(_SyncDevice);
            EditSettings = ReactiveCommand.Create(_EditSettings);
            SelectDevice = ReactiveCommand.Create<string, bool>(_SelectDevice);

            DevManager = new DevManager();
            DevManager.FindKindles();
        }

        #region properties
        public DevManager DevManager { get; set; }
        public IDevice SelectedDevice { get; }

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

        private string[] _Devices;
        public string[] Devices
        {
            get => _Devices;
            set { _Devices = value; }
        }

        private string _ConnectedDevice;
        public string ConnectedDevice
        {
            get => _ConnectedDevice;
            set { _ConnectedDevice = value; }
        }


        #endregion

        #region button commands

        public ReactiveCommand<string, bool> SelectDevice { get; set; }
        public bool _SelectDevice(string driveLetter)
        {
            bool setupRequired;
            try
            {
                setupRequired = DevManager.OpenDevice(driveLetter);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }

            if (setupRequired)
            {
                SetupDevice(DevManager.SelectedDevice);
            }
            return true;
        }

        public ReactiveCommand<Unit, Unit> EditSettings { get; set; }
        public void _EditSettings()
        {
            var dlg = new Dialogs.ConfigEditor();
            dlg.ShowDialog();
        }

        public ReactiveCommand<Unit, Unit> SyncDevice { get; set; }
        private void _SyncDevice()
        {
            if (DevManager.SelectedDevice == null)
            {
                MessageBox.Show("Connect to Kindle before syncing library");
                return;
            }
            var r = (BookBase)SelectedTableRow;
            DevManager.SelectedDevice.SendBook(r);
        }
        
        private void SetupDevice(IDevice kindle)
        {
            if (MessageBox.Show("It appears this is the first time you've used this device with KindleManager. " +
                "A new configuration will be created.", "Device Setup") == DialogResult.Cancel)
            {
                DevManager.SelectedDevice = null;
                return;
            };

            try
            {
                DeviceConfig c = new DeviceConfig();
                kindle.WriteConfig(c);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return;
            }

            kindle.Config = JsonConvert.DeserializeObject<DeviceConfig>(File.ReadAllText(kindle.ConfigFile));

            _EditDeviceSettings();
        }

        public ReactiveCommand<Unit, Unit> EditDeviceSettings { get; set; }
        private void _EditDeviceSettings()
        {
            if (DevManager.SelectedDevice == null) return;
            var dlg = new Dialogs.DeviceConfigEditor(DevManager.SelectedDevice);
            dlg.ShowDialog();
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

            throw new NotImplementedException();
            //string authorDir = Path.Combine(App.DataDir, importBook.Author);
            //Directory.CreateDirectory(authorDir);

            // Todo make filepath from config eg "{Author}/{Series}/{Title}.mobi"
            string destinationFile = ""; // Path.Combine(authorDir, importBook.Title) + ".mobi";

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
