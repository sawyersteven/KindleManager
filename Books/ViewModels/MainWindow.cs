using System;
using ReactiveUI;
using System.Reactive;
using System.Windows.Forms;
using Formats;
using System.Collections.ObjectModel;
using System.IO;
using Devices;
using System.Linq;
using ExtensionMethods;
using ReactiveUI.Fody.Helpers;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Books.ViewModels
{
    class MainWindow : ReactiveObject
    {
        public MainWindow()
        {
            BrowseForImport = ReactiveCommand.Create(_BrowseForImport);
            RemoveBook = ReactiveCommand.Create(_RemoveBook);
            EditMetadata = ReactiveCommand.Create(_EditMetadata);
            SendBook = ReactiveCommand.Create(_SendBook);
            EditSettings = ReactiveCommand.Create(_EditSettings);
            SelectDevice = ReactiveCommand.Create<string, bool>(_SelectDevice);
            EditDeviceSettings = ReactiveCommand.Create(_EditDeviceSettings);
            OpenDeviceFolder = ReactiveCommand.Create(_OpenDeviceFolder);
            OpenSideBar = ReactiveCommand.Create(_OpenSideBar);
            SyncDeviceLibrary = ReactiveCommand.Create(_SyncDeviceLibrary);

            DevManager = new DevManager();
            DevManager.FindKindles();

            TaskbarIcon = "None";
            TaskbarText = "";
            SideBarOpen = true;
        }

        #region properties
        [Reactive] public bool BackgroundWork { get; set; }
        [Reactive] public string TaskbarText { get; set; }
        [Reactive] public string TaskbarIcon { get; set; }

        public DevManager DevManager { get; set; }
        
        public ObservableCollection<Database.BookEntry> Library { get; } = App.Database.Library;

        [Reactive] public Database.BookEntry SelectedTableRow { get; set; }

        [Reactive] public Device SelectedDevice { get; set; }

        [Reactive] public bool SideBarOpen { get; set; }

        #endregion

        #region button commands
        public ReactiveCommand<Unit, Unit> SyncDeviceLibrary { get; set; }
        public void _SyncDeviceLibrary()
        {
            if (SelectedDevice == null)
            {
                // Todo alert
                return;
            }

            List<BookBase> toTransfer = new List<BookBase>();

            foreach (BookBase book in App.Database.Library)
            {
                if (!SelectedDevice.Database.Library.Any(x => x.Id == book.Id))
                {
                    toTransfer.Add(book);
                }
            }

            var dlg = new Dialogs.SyncConfirm(toTransfer, SelectedDevice);
            if (dlg.ShowDialog() == false)
            {
                return;
            }
            // TODO check toTransfer members
            Task.Run(() =>
            {
                TaskbarIcon = "";
                BackgroundWork = true;
                foreach (BookBase book in toTransfer)
                {
                    try
                    {
                        TaskbarText = $"Copying {book.Title}";
                        SelectedDevice.SendBook(book);
                    }
                    catch (Exception e)
                    {
                        TaskbarIcon = "AlertCircle";
                        TaskbarText = e.Message;
                    }
                    finally
                    {
                        
                    }
                }
                BackgroundWork = false;

            });
        }

        public ReactiveCommand<Unit, Unit> OpenSideBar { get; set; }
        public void _OpenSideBar()
        {
            SideBarOpen = true;
        }

        public ReactiveCommand<Unit, Unit> OpenDeviceFolder { get; set; }
        public void _OpenDeviceFolder()
        {
            if (SelectedDevice == null) return;
            string p = Path.Combine(SelectedDevice.DriveLetter, SelectedDevice.Config.LibraryRoot);
            while (!Directory.Exists(p))
            {
                p = Directory.GetParent(p).FullName;
            }
            System.Diagnostics.Process.Start(p);
        }

        public ReactiveCommand<string, bool> SelectDevice { get; set; }
        public bool _SelectDevice(string driveLetter)
        {
            try
            {
                SelectedDevice = DevManager.OpenDevice(driveLetter);
            }
            catch (Exception e)
            {
                var dlg = new Dialogs.Error("Unable to open Device", e.Message);
                dlg.ShowDialog();
                return false;
            }

            if (SelectedDevice.FirstUse)
            {
                if (!SetupDevice(SelectedDevice)) return false;
            }
            else
            {
                SelectedDevice.Init();
            }

            foreach(Database.BookEntry gridRow in App.Database.Library)
            {
                gridRow.OnDevice = SelectedDevice.Database.Library.Any(x => x.Id == gridRow.Id);
            }
            //App.Database.Library.CollectionChanged;
            return true;
        }

        public ReactiveCommand<Unit, Unit> EditSettings { get; set; }
        public void _EditSettings()
        {
            var dlg = new Dialogs.ConfigEditor();
            dlg.ShowDialog();
        }

        public ReactiveCommand<Unit, Unit> SendBook { get; set; }
        private void _SendBook()
        {
            if (SelectedDevice == null)
            {
                var dlg = new Dialogs.Error("No Kindle Selected", "Connect to Kindle Before Transferring Books");
                dlg.ShowDialog();
                return;
            }

            Task.Run(() =>
            {
                TaskbarIcon = "";
                BackgroundWork = true;
                var r = (BookBase)SelectedTableRow;

# if DEBUG
                Task.Delay(5000);
#endif
                try
                {
                    SelectedDevice.SendBook(r);
                }
                catch (Exception e)
                {
                    TaskbarIcon = "AlertCircle";
                    TaskbarText = e.Message;
                }
                finally
                {
                    BackgroundWork = false;
                }
            });
        }
        
        public ReactiveCommand<Unit, Unit> EditDeviceSettings { get; set; }
        private void _EditDeviceSettings()
        {
            if (SelectedDevice == null) return;
            var dlg = new Dialogs.DeviceConfigEditor(SelectedDevice);
            dlg.ShowDialog();
        }

        public ReactiveCommand<Unit, Unit> BrowseForImport { get; set; }
        private void _BrowseForImport()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = false;
            dlg.Filter = "eBooks (*.epub;*.mobi)|*.epub;*.mobi";

            if (dlg.ShowDialog() != DialogResult.OK) return;

            BookBase importBook;
            try
            {
                importBook = Converters.NewIBook(dlg.FileName);
            }
            catch (Exception e)
            {
                var errdlg = new Dialogs.Error("Error Opening Book", e.Message);
                errdlg.ShowDialog();
                return;
            }
            ImportBook(importBook);

        }

        public ReactiveCommand<Unit, Unit> RemoveBook { get; set; }
        private void _RemoveBook()
        {
            if (SelectedTableRow == null) { return; }

            string msg = $"Are you sure you want to remove {SelectedTableRow.Title} from your library?";
            var dlg = new Dialogs.YesNo("Confirm Remove", msg, "Remove");

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
            BookBase book = null;
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

        //public ReactiveCommand<Unit, Unit> 
        #endregion

        private bool SetupDevice(Device kindle)
        {
            var dlg = new Dialogs.YesNo("Device Setup", "It appears this is the first time you've used this device with KindleManager. " +
                "A new configuration will be created.");
            if (dlg.ShowDialog() == false)
            {
                SelectedDevice = null;
                return false;
            }

            DeviceConfig c;
            try
            {
                c = new DeviceConfig();
                kindle.WriteConfig(c);
            }
            catch (Exception e)
            {
                var errdlg = new Dialogs.Error("Error Creating Config File", e.Message);
                errdlg.ShowDialog();
                return false;
            }

            kindle.Config = c;
            _EditDeviceSettings();

            // Setup directories
            try
            {
                Directory.CreateDirectory(Path.Combine(SelectedDevice.DriveLetter, SelectedDevice.Config.LibraryRoot));
            }
            catch (Exception e)
            {
                var errdlg = new Dialogs.Error("Error Creating Library Structure", e.Message);
                errdlg.ShowDialog();
                return false;
            }

            // Device DB
            try
            {
                SelectedDevice.Database = new Database(Path.Combine(SelectedDevice.DriveLetter, "KindleManager.db"));
            }
            catch (Exception e)
            {
                var errdlg = new Dialogs.Error("Error Creating Device Database", e.Message);
                errdlg.ShowDialog();
                return false;
            }
            return true;
        }

        private void ImportBook(BookBase book)
        {
            string destinationFile = App.ConfigManager.config.LibraryFormat.DictFormat(book.Props());
            destinationFile = Path.Combine(App.ConfigManager.config.LibraryDir, destinationFile, book.Title) + ".mobi";

            if (Path.GetExtension(book.FilePath) != ".mobi")
            {
                string msg = $"{book.FilePath} is not compatible and must be converted to mobi before adding to library.";

                var convertdlg = new Dialogs.YesNo(Path.GetFileName(book.FilePath), msg, "Convert");
                if (convertdlg.ShowDialog() == false) return;
                try
                {
                    book = Converters.ToMobi(book, destinationFile);
                }
                catch (Exception e)
                {
                    var errdlg = new Dialogs.Error("Conversion Failed", e.Message);
                    errdlg.ShowDialog();
                    return;
                }
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(Directory.GetParent(destinationFile).FullName);
                    File.Copy(book.FilePath, destinationFile);
                }
                catch (Exception e)
                {
                    var errdlg = new Dialogs.Error("Copy File Error", e.Message);
                    errdlg.ShowDialog();
                    return;
                }
            }

            try
            {
                App.Database.AddBook(book);
            }
            catch (InvalidOperationException e)
            {
                var errdlg = new Dialogs.Error("Could Not Write To Database", e.Message);
                errdlg.ShowDialog();

                try
                {
                    File.Delete(destinationFile);
                }
                catch { }

                return;
            }
            catch (Exception e)
            {
                var errdlg = new Dialogs.Error("Could Not Write To Database", e.Message);
                errdlg.ShowDialog();
                return;
            }
        }
    }
}
