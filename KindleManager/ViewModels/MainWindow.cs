using Devices;
using Formats;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KindleManager.ViewModels
{
    class MainWindow : ReactiveObject
    {
        private readonly Unit UnitNull = new Unit();

        public MainWindow()
        {
            BrowseForImport = ReactiveCommand.Create(_BrowseForImport);
            RemoveBook = ReactiveCommand.Create(_RemoveBook, this.WhenAnyValue(vm => vm.ButtonEnable));
            EditMetadata = ReactiveCommand.Create(_EditMetadata, this.WhenAnyValue(vm => vm.ButtonEnable));
            OpenSideBar = ReactiveCommand.Create(_OpenSideBar);
            OpenBookFolder = ReactiveCommand.Create(_OpenBookFolder);
            SendBook = ReactiveCommand.Create<IList, Unit>(_SendBook, this.WhenAnyValue(vm => vm.ButtonEnable));
            EditSettings = ReactiveCommand.Create(_EditSettings);
            ReceiveBook = ReactiveCommand.Create(_ReceiveBook, this.WhenAnyValue(vm => vm.ButtonEnable));

            #region device buttonss
            SelectDevice = ReactiveCommand.Create<string, bool>(_SelectDevice, this.WhenAnyValue(vm => vm.ButtonEnable));
            EditDeviceSettings = ReactiveCommand.Create(_EditDeviceSettings, this.WhenAnyValue(vm => vm.ButtonEnable));
            OpenDeviceFolder = ReactiveCommand.Create(_OpenDeviceFolder);
            SyncDeviceLibrary = ReactiveCommand.Create(_SyncDeviceLibrary, this.WhenAnyValue(vm => vm.ButtonEnable));
            ReorganizeLibrary = ReactiveCommand.Create(_ReorganizeLibrary, this.WhenAnyValue(vm => vm.ButtonEnable));
            RecreateLibrary = ReactiveCommand.Create(_RecreateLibrary, this.WhenAnyValue(vm => vm.ButtonEnable));

            #endregion

            DevManager = new DevManager();
            DevManager.FindKindles();

            TaskbarIcon = "None";
            TaskbarText = "";
            SideBarOpen = true;
            BackgroundWork = false;
        }

        #region properties
        private bool _BackgroundWork;
        public bool BackgroundWork
        {
            get => _BackgroundWork;
            set
            {
                this.RaiseAndSetIfChanged(ref _BackgroundWork, value);
                ButtonEnable = !value;
            }
        }
        [Reactive] public bool ButtonEnable { get; set; }
        [Reactive] public string TaskbarText { get; set; }
        [Reactive] public string TaskbarIcon { get; set; }
        public DevManager DevManager { get; set; }
        [Reactive] public Database.BookEntry SelectedTableRow { get; set; }
        [Reactive] public Device SelectedDevice { get; set; }
        [Reactive] public bool SideBarOpen { get; set; }
        public ObservableCollection<Database.BookEntry> LocalLibrary { get; set; } = App.Database.Library;
        [Reactive] public ObservableCollection<Database.BookEntry> RemoteLibrary { get; set; } = new ObservableCollection<Database.BookEntry>();
        #endregion

        #region button commands
        public ReactiveCommand<Unit, Unit> ReceiveBook { get; set; }
        public void _ReceiveBook()
        {

        }

        public ReactiveCommand<Unit, Unit> ReorganizeLibrary { get; set; }
        public void _ReorganizeLibrary()
        {
            var dlg = new Dialogs.YesNo("Reorganize Library", "All books in your Kindle's library will be moved and renamed according to your Kindle's settings. This may take some time depending on the size of your library.");
            dlg.ShowDialog();
            if (dlg.DialogResult == false) return;

            Task.Run(() =>
            {
                BackgroundWork = true;
                TaskbarIcon = "None";
                try
                {
                    foreach (string title in SelectedDevice.ReorganizeLibrary())
                    {
                        TaskbarText = $"Processing {title}";
                    }
                    SelectedDevice.CleanLibrary();
                }
                catch (AggregateException e)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        new Dialogs.BulkProcessErrors("The following errors occurred while reorganizing your library.", e.InnerExceptions.ToArray()).ShowDialog();
                    });
                }
                catch (Exception e)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        new Dialogs.Error("Processing Error", e.Message).ShowDialog();
                    });
                }
                finally
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        BackgroundWork = false;
                        TaskbarText = "Kindle library reorganization complete.";
                        TaskbarIcon = "CheckCircle";
                    });
                }
            });
        }

        public ReactiveCommand<Unit, Unit> RecreateLibrary { get; set; }
        public void _RecreateLibrary()
        {
            var dlg = new Dialogs.YesNo("Recreate Library", "Your Kindle will be scanned for books which will then be organized and renamed according to your Kindle's settings.");
            dlg.ShowDialog();
            if (dlg.DialogResult == false) return;

            BackgroundWork = true;
            TaskbarIcon = "None";
            Task.Run(() =>
            {
                try
                {
                    foreach (string title in SelectedDevice.RecreateLibraryAndDatabse())
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            TaskbarText = $"Processing {title}";
                        });
                    }
                }
                catch (AggregateException e)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        new Dialogs.BulkProcessErrors("The following errors occurred while rebuilding your library.", e.InnerExceptions.ToArray()).ShowDialog();
                    });
                }
                catch (Exception e)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        new Dialogs.Error("Recreating Library Failed", e.Message);
                    });
                }
                finally
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        BackgroundWork = false;
                        TaskbarText = "Kindle library recreation complete.";
                        TaskbarIcon = "CheckCircle";
                    });
                }
            });
        }

        public ReactiveCommand<Unit, Unit> SyncDeviceLibrary { get; set; }
        public void _SyncDeviceLibrary()
        {
            if (SelectedDevice == null)
            {
                new Dialogs.Error("No Kindle Selected", "Connect to Kindle Before Transferring Books").ShowDialog();
                return;
            }

            List<Database.BookEntry> toTransfer = new List<Database.BookEntry>();

            foreach (Database.BookEntry book in App.Database.Library)
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

            var a = dlg.UserSelectedBooks;

            foreach (var b in dlg.UserSelectedBooks)
            {
                if (!b.Checked)
                {
                    Database.BookEntry t = toTransfer.FirstOrDefault(x => x.Id == b.Id);
                    if (t != null) toTransfer.Remove(t);
                }
            }

            Task.Run(() =>
            {
                List<Exception> errors = new List<Exception>();
                TaskbarIcon = "";
                BackgroundWork = true;
                foreach (Database.BookEntry book in toTransfer)
                {
                    try
                    {
                        TaskbarText = $"Copying {book.Title}";
                        SelectedDevice.SendBook(book);
                    }
                    catch (Exception e)
                    {
                        errors.Add(e);
                    }
                }
                TaskbarText = $"Sync Complete";
                TaskbarIcon = "CheckCircle";
                BackgroundWork = false;

                if (errors.Count > 0)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        new Dialogs.BulkProcessErrors("The following errors occurred while syncing your Kindle library.", errors.ToArray()).ShowDialog();
                    });
                }
            });
        }

        public ReactiveCommand<Unit, Unit> OpenSideBar { get; set; }
        public void _OpenSideBar()
        {
            SideBarOpen = true;
            Task.Run(() =>
            {
                BackgroundWork = true;
                DevManager.FindKindles();
                BackgroundWork = false;
            });
        }

        public ReactiveCommand<Unit, Unit> OpenBookFolder { get; set; }
        public void _OpenBookFolder()
        {
            if (SelectedTableRow == null) return;
            System.Diagnostics.Process.Start(Path.GetDirectoryName(SelectedTableRow.FilePath));
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
            RemoteLibrary = SelectedDevice.Database.Library;

            return true;
        }

        public ReactiveCommand<Unit, Unit> EditSettings { get; set; }
        public void _EditSettings()
        {
            var dlg = new Dialogs.ConfigEditor();
            dlg.ShowDialog();
        }

        /* This method is set up to be able to use multiple selections in the
         * LibraryTable datagrid. It current only uses the first and is an 
         * example for how to convert other methods to accept multiple
         * datagrid selections. 
         */
        public ReactiveCommand<IList, Unit> SendBook { get; set; }
        public Unit _SendBook(IList p)
        {
            if (SelectedDevice == null)
            {
                new Dialogs.Error("No Kindle Selected", "Connect to Kindle Before Transferring Books").ShowDialog();
                return UnitNull;
            }

            TaskbarIcon = "";
            BackgroundWork = true;
            Task.Run(() =>
            {
                var r = (Database.BookEntry)p[0];
                try
                {
                    SelectedDevice.SendBook(r);
                    TaskbarIcon = "CheckCircle";
                    TaskbarText = $"{r.Title} sent to {SelectedDevice.Name}.";
                }
                catch (Exception e)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        TaskbarIcon = "AlertCircle";
                        TaskbarText = e.Message;
                    });
                }
                finally
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        BackgroundWork = false;
                    });

                }
            });
            return UnitNull;
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

            string types = string.Join(";*", Formats.Resources.AcceptedFileTypes);
            dlg.Filter = $"eBooks (*{types})|*{types}";

            if (dlg.ShowDialog() != DialogResult.OK) return;

            if (Path.GetExtension(dlg.FileName) != ".mobi")
            {
                string msg = $"{dlg.FileName} is not compatible and must be converted to mobi before adding to library.";
                var convertdlg = new Dialogs.YesNo(Path.GetFileName(dlg.FileName), msg, "Convert");
                if (convertdlg.ShowDialog() == false) return;
            }

            ImportBook(dlg.FileName);
        }

        public ReactiveCommand<Unit, Unit> RemoveBook { get; set; }
        public void _RemoveBook()
        {
            if (SelectedTableRow == null) { return; }

            // TODO new dialog asking to remove from kindle or pc or both.

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
        public void _EditMetadata()
        {
            if (SelectedTableRow == null) return;
            BookBase book;
            try
            {
                book = Formats.BookBase.Auto(SelectedTableRow.FilePath);
            }
            catch (Exception e)
            {
                new Dialogs.Error("Unable to open book for editing", e.Message).ShowDialog();
                return;
            }

            var dlg = new Dialogs.MetadataEditor(new Database.BookEntry(SelectedTableRow));
            dlg.ShowDialog();
            if (dlg.DialogResult == false) return;
            BookBase.Merge(dlg.ModBook, book);
            App.Database.UpdateBook(dlg.ModBook);
        }
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

            // TODO ask to scan
            return true;
        }

        private void ImportBook(string filePath)
        {
            try
            {
                BackgroundWork = true;
                Library.ImportBook(filePath);
            }
            catch (LiteDB.LiteException e)
            {
                new Dialogs.Error("Unable to write to database", e.Message).ShowDialog();
                return;
            }
            catch (Exception e)
            {
                new Dialogs.Error("Import Error", e.Message).ShowDialog();
                return;
            }
            finally
            {
                BackgroundWork = false;
            }
        }

        public void ImportBooksDrop(string path)
        {
            if (File.Exists(path))
            {
                if (!Formats.Resources.AcceptedFileTypes.Contains(Path.GetExtension(path)))
                {
                    new Dialogs.Error("Incompatible Format", $"Importing {Path.GetExtension(path)} format books has not yet been implemented.").ShowDialog();
                    return;
                }
                ImportBook(path);
            }
            else if (Directory.Exists(path))
            {
                var dlg = new Dialogs.BulkImport(path);
                dlg.ShowDialog();
                if (dlg.DialogResult == false) return;

                string[] files = dlg.SelectedFiles();

                Task.Run(() =>
                {
                    List<Exception> errors = new List<Exception>();

                    foreach (string file in files)
                    {
                        TaskbarText = $"Importing {file}";
                        try
                        {
                            Library.ImportBook(file);
                        }
                        catch (Exception e)
                        {
                            e.Data["File"] = file;
                            errors.Add(e);
                        }
                    }

                    if (errors.Count > 0)
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            new Dialogs.BulkProcessErrors("The following errors occurred while adding to your library.", errors.ToArray()).ShowDialog();
                        });
                    }

                    TaskbarText = "Import Complete";
                    TaskbarIcon = "CheckCircle";

                });
                return;
            }
        }

        public void SaveLibraryColumns(List<string> hiddenColumns)
        {
            if (hiddenColumns.SequenceEqual(App.ConfigManager.config.HiddenColumns)) return;

            App.ConfigManager.config.HiddenColumns = hiddenColumns.ToList();
            App.ConfigManager.Write();
        }
    }
}
