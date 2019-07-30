using ExtensionMethods;
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
    static class Icons
    {
        public static string None = "None";
        public static string Check = "Check";
        public static string Alert = "Alert";
        public static string Clone = "RepoClone";
    }

    class MainWindow : ReactiveObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            BrowseForImport = ReactiveCommand.Create(_BrowseForImport);
            RemoveBook = ReactiveCommand.Create(_RemoveBook, this.WhenAnyValue(vm => vm.ButtonEnable, vm => vm.SelectedTableRow, (b, n) => b && n != null));
            EditMetadata = ReactiveCommand.Create(_EditMetadata, this.WhenAnyValue(vm => vm.ButtonEnable, vm => vm.SelectedTableRow, (b, n) => b && n != null));
            OpenSideBar = ReactiveCommand.Create(_OpenSideBar);
            OpenBookFolder = ReactiveCommand.Create(_OpenBookFolder);
            SendBook = ReactiveCommand.Create<IList, Unit>(_SendBook, this.WhenAnyValue(vm => vm.ButtonEnable, vm => vm.SelectedTableRow, vm => vm.SelectedDevice, (b, n, m) => b && n != null && m != null));
            EditSettings = ReactiveCommand.Create(_EditSettings);
            ReceiveBook = ReactiveCommand.Create<IList, Unit>(_ReceiveBook, this.WhenAnyValue(vm => vm.ButtonEnable));

            #region device buttons
            SelectDevice = ReactiveCommand.Create<string, Task<bool>>(_SelectDevice, this.WhenAnyValue(vm => vm.ButtonEnable));
            EditDeviceSettings = ReactiveCommand.Create<bool, Task<Unit>>(_EditDeviceSettings, this.WhenAnyValue(vm => vm.ButtonEnable));
            OpenDeviceFolder = ReactiveCommand.Create(_OpenDeviceFolder);
            SyncDeviceLibrary = ReactiveCommand.Create(_SyncDeviceLibrary, this.WhenAnyValue(vm => vm.ButtonEnable));
            ReorganizeDeviceLibrary = ReactiveCommand.Create(_ReorganizeDeviceLibrary, this.WhenAnyValue(vm => vm.ButtonEnable));
            ScanDeviceLibrary = ReactiveCommand.Create(_ScanDeviceLibrary, this.WhenAnyValue(vm => vm.ButtonEnable));
            CloseDevice = ReactiveCommand.Create(_CloseDevice, this.WhenAnyValue(vm => vm.ButtonEnable));

            #endregion

            ShowAbout = ReactiveCommand.Create(_ShowAbout);

            snackBarQueue = new MaterialDesignThemes.Wpf.SnackbarMessageQueue();
            DevManager = new Devices.DevManager();

            StatusBarIcon = Icons.None;
            StatusBarText = "";
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
        [Reactive] public string StatusBarText { get; set; }
        [Reactive] public string StatusBarIcon { get; set; }
        public Devices.DevManager DevManager { get; set; }
        [Reactive] public Database.BookEntry SelectedTableRow { get; set; }
        [Reactive] public Devices.DeviceBase SelectedDevice { get; set; }
        [Reactive] public bool SideBarOpen { get; set; }
        public ObservableCollection<Database.BookEntry> LocalLibrary { get; set; } = App.LocalLibrary.Database.BOOKS;
        [Reactive] public ObservableCollection<Database.BookEntry> RemoteLibrary { get; set; } = new ObservableCollection<Database.BookEntry>();

        public MaterialDesignThemes.Wpf.SnackbarMessageQueue snackBarQueue { get; set; }

        #endregion

        #region button commands

        public ReactiveCommand<IList, Unit> ReceiveBook { get; set; }
        public Unit _ReceiveBook(IList bookList)
        {
            if (bookList.Count == 0) return Unit.Default;

            Database.BookEntry[] dbRows = new Database.BookEntry[bookList.Count];
            bookList.CopyTo(dbRows, 0);

            Task.Run(() =>
            {
                if (bookList.Count == 1)
                {
                    Database.BookEntry book = (Database.BookEntry)bookList[0];
                    try
                    {
                        book.FilePath = SelectedDevice.AbsoluteFilePath(book);
                        ImportBook(book);
                    }
                    catch (Exception e)
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            var dlg = new Dialogs.Error("Error transferring book", e.Message);
                            MaterialDesignThemes.Wpf.DialogHost.Show(dlg);
                        });
                        return;
                    }
                    snackBarQueue.Enqueue($"{book.Title} copied to library.");
                }
                else
                {
                    List<Exception> errs = new List<Exception>();

                    var prgDlg = new Dialogs.Progress("Syncing Library", false);
                    _ = MaterialDesignThemes.Wpf.DialogHost.Show(prgDlg);

                    int step = 100 / bookList.Count;

                    foreach (Database.BookEntry b in bookList)
                    {
                        Database.BookEntry book = new Database.BookEntry(b);
                        try
                        {

                            book.FilePath = SelectedDevice.AbsoluteFilePath(book);
                            ImportBook(book);
                        }
                        catch (Exception e)
                        {
                            e.Data["item"] = book.Title;
                            errs.Add(e);
                        }
                    }

                    if (errs.Count > 0)
                    {
                        prgDlg.ShowError(new AggregateException(errs.ToArray()));
                    }
                }
            });

            return Unit.Default;
        }

        public ReactiveCommand<Unit, Unit> ReorganizeDeviceLibrary { get; set; }
        public async void _ReorganizeDeviceLibrary()
        {
            var dlg = new Dialogs.YesNo("Reorganize Library", "All books in your Kindle's library will be moved and renamed according to your Kindle's settings. This may take some time depending on the size of your library.");
            await MaterialDesignThemes.Wpf.DialogHost.Show(dlg);
            if (dlg.DialogResult == false) return;

            var prgDlg = new Dialogs.Progress("Reorganizing Library", true);
            _ = MaterialDesignThemes.Wpf.DialogHost.Show(prgDlg);

            _ = Task.Run(() =>
              {
                  try
                  {
                      foreach (BookBase book in SelectedDevice.Reorganize())
                      {
                          prgDlg.Current = $"Processed {book.Title}";
                      }
                      prgDlg.Current = "Cleaning up...";
                      SelectedDevice.Clean();
                  }
                  catch (Exception e)
                  {
                      prgDlg.ShowError(e);
                  }
                  finally
                  {
                      prgDlg.Finish($"{SelectedDevice.Name} reorganized.");
                  }
              });
        }

        public ReactiveCommand<Unit, Unit> ScanDeviceLibrary { get; set; }
        public async void _ScanDeviceLibrary()
        {
            StatusBarIcon = Icons.None;

            var dlg = new Dialogs.YesNo("Rescan Library", "Your Kindle will be scanned for books which will then be organized and renamed according to your Kindle's settings.");
            await MaterialDesignThemes.Wpf.DialogHost.Show(dlg);
            if (dlg.DialogResult == false) return;

            var prgDlg = new Dialogs.Progress("Scanning Library", true);
            _ = MaterialDesignThemes.Wpf.DialogHost.Show(prgDlg);

            _ = Task.Run(() =>
            {
                try
                {
                    foreach (BookBase book in SelectedDevice.Rescan())
                    {
                        prgDlg.Current = $"Processed {book.Title}";
                    }
                }
                catch (Exception e)
                {
                    prgDlg.ShowError(e);
                }
                finally
                {
                    prgDlg.Finish($"{SelectedDevice.Name} library scan complete.");
                }
            });
        }

        public ReactiveCommand<Unit, Unit> SyncDeviceLibrary { get; set; }
        public async void _SyncDeviceLibrary()
        {
            if (SelectedDevice == null)
            {
                var errDlg = new Dialogs.Error("No Kindle Selected", "Connect to Kindle Before Transferring Books");
                await MaterialDesignThemes.Wpf.DialogHost.Show(errDlg);
                return;
            }

            List<Database.BookEntry> toTransfer = new List<Database.BookEntry>();

            foreach (Database.BookEntry book in App.LocalLibrary.Database.BOOKS)
            {
                if (!SelectedDevice.Database.BOOKS.Any(x => x.Id == book.Id))
                {
                    toTransfer.Add(book);
                }
            }

            var dlg = new Dialogs.SyncConfirm(toTransfer, SelectedDevice.Name);
            await MaterialDesignThemes.Wpf.DialogHost.Show(dlg);
            if (dlg.DialogResult == false)
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

            var prgDlg = new Dialogs.Progress("Syncing Kindle Library", false);
            _ = MaterialDesignThemes.Wpf.DialogHost.Show(prgDlg);


            _ = Task.Run(() =>
             {
                 List<Exception> errors = new List<Exception>();
                 AggregateException errs = new AggregateException();
                 int step = 100 / toTransfer.Count;
                 for (int i = 0; i < toTransfer.Count; i++)
                 {
                     Database.BookEntry book = toTransfer[i];
                     try
                     {
                         prgDlg.Current = $"Copying {book.Title}";
                         prgDlg.Percent += step;
                         book.FilePath = App.LocalLibrary.AbsoluteFilePath(book);
                         SelectedDevice.ImportBook(book);
                     }
                     catch (Exception e)
                     {
                         e.Data.Add("item", book.Title);
                         errors.Add(e);
                     }
                 }

                 prgDlg.Finish("Library sync complete");
                 if (errors.Count > 0)
                 {
                     prgDlg.ShowError(new AggregateException(errors.ToArray()));
                 }
                 else
                 {
                     prgDlg.Close(this, null);
                     snackBarQueue.Enqueue($"{SelectedDevice.Name} library synced");
                 }
             });
        }

        public ReactiveCommand<Unit, Unit> OpenSideBar { get; set; }
        public void _OpenSideBar()
        {
            BackgroundWork = true;
            Task.Run(() =>
            {
                DevManager.FindDevices();
            });
        }

        public ReactiveCommand<Unit, Unit> OpenBookFolder { get; set; }
        public void _OpenBookFolder()
        {
            if (SelectedTableRow == null) return;
            try
            {
                System.Diagnostics.Process.Start(Path.GetDirectoryName(App.LocalLibrary.AbsoluteFilePath(SelectedTableRow)));
            }
            catch (System.ComponentModel.Win32Exception)
            {
                if (SelectedDevice != null)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(Path.GetDirectoryName(SelectedDevice.AbsoluteFilePath(SelectedTableRow)));
                    }
                    catch { }
                }
            }
        }

        public ReactiveCommand<Unit, Unit> OpenDeviceFolder { get; set; }
        public void _OpenDeviceFolder()
        {
            if (SelectedDevice == null) return;
            string p = SelectedDevice.LibraryRoot;
            while (!Directory.Exists(p))
            {
                p = Directory.GetParent(p).FullName;
            }
            System.Diagnostics.Process.Start(p);
        }

        public ReactiveCommand<string, Task<bool>> SelectDevice { get; set; }
        public async Task<bool> _SelectDevice(string driveLetter)
        {
            if (BackgroundWork) return false;
            try
            {
                SelectedDevice = DevManager.OpenDevice(driveLetter);
            }
            catch (Exception e)
            {
                var errDlg = new Dialogs.Error("Unable to open Device", e.Message);
                await MaterialDesignThemes.Wpf.DialogHost.Show(errDlg);
                return false;
            }

            if (SelectedDevice.Open())
            {
                var dlg = new Dialogs.YesNo("Device Setup", "It appears this is the first time you've used this device with KindleManager. A new configuration and database will be created.");
                await MaterialDesignThemes.Wpf.DialogHost.Show(dlg);
                if (dlg.DialogResult == false)
                {
                    SelectedDevice = null;
                    return false;
                }

                await _EditDeviceSettings(true);
                _ScanDeviceLibrary();
            }

            RemoteLibrary = SelectedDevice.Database.BOOKS;

            return true;
        }

        public ReactiveCommand<Unit, Unit> EditSettings { get; set; }
        public async void _EditSettings()
        {
            var dlg = new Dialogs.ConfigEditor();
            await MaterialDesignThemes.Wpf.DialogHost.Show(dlg);
        }

        /* This method is set up to be able to use multiple selections in the
         * LibraryTable datagrid. It current only uses the first and is an 
         * example for how to convert other methods to accept multiple
         * datagrid selections. 
         */
        public ReactiveCommand<IList, Unit> SendBook { get; set; }
        public Unit _SendBook(IList bookList)
        {
            if (SelectedDevice == null)
            {
                var errDlg = new Dialogs.Error("No Kindle Selected", "Connect to Kindle Before Transferring Books");
                MaterialDesignThemes.Wpf.DialogHost.Show(errDlg);
                return Unit.Default;
            }

            Task.Run(() =>
            {
                if (bookList.Count == 1)
                {
                    try
                    {
                        BookBase book = (BookBase)bookList[0];
                        book.FilePath = App.LocalLibrary.AbsoluteFilePath(book);
                        SelectedDevice.ImportBook(book);
                        snackBarQueue.Enqueue($"{book.Title} transferred to {SelectedDevice.Name}");
                    }
                    catch (Exception e)
                    {
                        var dlg = new Dialogs.Error("Error transferring book", e.Message);
                    }
                }
                else
                {
                    List<Exception> errs = new List<Exception>();

                    var prgDlg = new Dialogs.Progress("Syncing Library", false);
                    _ = MaterialDesignThemes.Wpf.DialogHost.Show(prgDlg);

                    int step = 100 / bookList.Count;

                    foreach (Database.BookEntry book in bookList)
                    {
                        try
                        {
                            book.FilePath = App.LocalLibrary.AbsoluteFilePath(book);
                            SelectedDevice.ImportBook(book);
                            prgDlg.Current = $"Transferred {book.Title}";
                        }
                        catch (Exception e)
                        {
                            e.Data["item"] = book.Title;
                            errs.Add(e);
                        }
                        finally
                        {
                            prgDlg.Percent += step;
                        }
                    }
                    prgDlg.Finish("Book transfer complete.");
                    if (errs.Count > 0)
                    {
                        prgDlg.ShowError(new AggregateException(errs.ToArray()));
                    }
                }
            });
            return Unit.Default;
        }

        public ReactiveCommand<bool, Task<Unit>> EditDeviceSettings { get; set; }
        /// <summary>
        /// Pass prompt=false to disable asking to reorganize library
        /// </summary>
        /// <remarks>
        /// This method is a bit wonky but I wanted to lay the basics
        /// for handling different kinds of devices in case it is needed
        /// in the future.
        /// </remarks>
        private async Task<Unit> _EditDeviceSettings(bool skipPrompt = false)
        {
            if (SelectedDevice == null) return Unit.Default;

            if (SelectedDevice is Devices.FSDevice d)
            {
                var dlg = new Dialogs.FSDeviceConfigEditor(d.Config);
                await MaterialDesignThemes.Wpf.DialogHost.Show(dlg);
                if (dlg.DialogResult == false) return Unit.Default;
                d.Config.CopyFrom(dlg.Config);

                if (!skipPrompt && dlg.RequestReorg)
                {
                    var dlg2 = new Dialogs.YesNo("Reorganize Library", "You have changed your device library's Directory Format. Would you like to reorganize your library now?", "Reorganize");
                    await MaterialDesignThemes.Wpf.DialogHost.Show(dlg2);
                    if (dlg2.DialogResult == true)
                    {
                        _ReorganizeDeviceLibrary();
                    }
                }
            }
            return Unit.Default;
        }

        public ReactiveCommand<Unit, Unit> BrowseForImport { get; set; }
        private void _BrowseForImport()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = false;

            string types = string.Join(";*", Formats.Resources.AcceptedFileTypes);
            dlg.Filter = $"eBooks (*{types})|*{types}";

            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                ImportBook(dlg.FileName);
            }
            catch (Exception e)
            {
                var errDlg = new Dialogs.Error("Import failed", e.Message);
                _ = MaterialDesignThemes.Wpf.DialogHost.Show(errDlg);
            }
        }

        public ReactiveCommand<Unit, Unit> RemoveBook { get; set; }
        public async void _RemoveBook()
        {
            if (SelectedTableRow == null) { return; }
            string title = SelectedTableRow.Title;

            bool onDevice = SelectedDevice == null ? false : SelectedDevice.Database.BOOKS.Any(x => x.Id == SelectedTableRow.Id);
            bool onPC = LocalLibrary.Any(x => x.Id == SelectedTableRow.Id);


            var dlg = new Dialogs.DeleteConfirm(SelectedTableRow.Title, onDevice, onPC);
            await MaterialDesignThemes.Wpf.DialogHost.Show(dlg);

            if (dlg.DialogResult == false) { return; }
            if (dlg.DeleteFrom == 0 || dlg.DeleteFrom == 1) // device
            {
                Database.BookEntry remoteBook = SelectedDevice.Database.BOOKS.FirstOrDefault(x => x.Id == SelectedTableRow.Id);
                try
                {
                    SelectedDevice.DeleteBook(SelectedTableRow.Id);
                }
                catch (Exception e)
                {
                    var errDlg = new Dialogs.Error($"Unable to delete book from {SelectedDevice.Name}", e.Message);
                    await MaterialDesignThemes.Wpf.DialogHost.Show(errDlg);
                    return;
                }
            }
            if (dlg.DeleteFrom == 0 || dlg.DeleteFrom == 2) // pc
            {
                Database.BookEntry localBook = LocalLibrary.FirstOrDefault(x => x.Id == SelectedTableRow.Id);
                try
                {
                    if (localBook != null)
                    {
                        try
                        {
                            File.Delete(localBook.FilePath);
                        }
                        catch (FileNotFoundException) { }
                        catch (DirectoryNotFoundException) { }

                        Utils.Files.CleanBackward(Path.GetDirectoryName(localBook.FilePath), App.LibraryDirectory);
                    }
                    App.LocalLibrary.Database.RemoveBook(SelectedTableRow);
                }
                catch (Exception e)
                {
                    var errDlg = new Dialogs.Error("Unable to delete book from library", e.Message);
                    await MaterialDesignThemes.Wpf.DialogHost.Show(errDlg);
                    return;
                }
            }

            string msg = dlg.DeleteFrom == 0 ? "PC & Kindle" : (dlg.DeleteFrom == 2 ? "PC" : "Kindle");
            snackBarQueue.Enqueue($"{title} deleted from {msg}.");
        }

        public ReactiveCommand<Unit, Unit> EditMetadata { get; set; }
        public async void _EditMetadata()
        {
            if (SelectedTableRow == null) return;

            var dlg = new Dialogs.MetadataEditor(new Database.BookEntry(SelectedTableRow));
            await MaterialDesignThemes.Wpf.DialogHost.Show(dlg);
            if (dlg.DialogResult == false) return;

            List<Exception> errs = new List<Exception>();

            string title = null;
            Database.BookEntry bookEntry;
            bookEntry = LocalLibrary.FirstOrDefault(x => x.Id == SelectedTableRow.Id);
            if (bookEntry != null)
            {
                title = bookEntry.Title;
                try
                {
                    App.LocalLibrary.UpdateBookMetadata(dlg.ModBook);
                }
                catch (Exception e)
                {
                    errs.Add(e);
                }
            }

            bookEntry = RemoteLibrary.FirstOrDefault(x => x.Id == SelectedTableRow.Id);

            if (bookEntry != null)
            {
                if (title == null) title = bookEntry.Title;
                try
                {
                    SelectedDevice.UpdateBookMetadata(dlg.ModBook);
                }
                catch (Exception e)
                {
                    errs.Add(e);
                }
            }

            if (errs.Count != 0)
            {
                string msg = $"Metadata could not be updated.&#x0a; {string.Join("; ", errs.Select(x => x.Message).ToList())}";
                var errDlg = new Dialogs.Error("Error updating metadata", msg);
                _ = MaterialDesignThemes.Wpf.DialogHost.Show(errDlg);
            }
            else if (title != null)
            {
                snackBarQueue.Enqueue($"{title} updated");
            }

        }

        public ReactiveCommand<Unit, Unit> CloseDevice { get; set; }
        public void _CloseDevice()
        {
            SelectedDevice = null;
            RemoteLibrary.Clear();
        }

        #endregion

        public ReactiveCommand<Unit, Unit> ShowAbout { get; set; }
        public void _ShowAbout()
        {
            var dlg = new Dialogs.Splash();
            MaterialDesignThemes.Wpf.DialogHost.Show(dlg);
        }

        /// <summary>
        /// Used to import from another device's library.
        /// </summary>
        private void ImportBook(Database.BookEntry remoteEntry)
        {
            Logger.Info("Importing {}.", remoteEntry.Title);
            BookBase localBook = LocalLibrary.FirstOrDefault(x => x.Id == remoteEntry.Id);
            if (localBook != null)
            {
                Logger.Info("{}[{}] already exists in library, copying metadata from Kindle.", localBook.Title, localBook.Id);
                try
                {
                    localBook.UpdateMetadata(remoteEntry);
                }
                catch (LiteDB.LiteException e)
                {
                    Logger.Error(e, "Unable to update metadata in database.");
                    throw new Exception($"Unable to update metadata in database; {e.Message}");
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Unable to write metadata to disk.");
                    throw new Exception($"Unable to write metadata to disk; {e.Message}");
                }
            }

            Dictionary<string, string> remoteMetadata = remoteEntry.Props();

            string localFile = Path.Combine(App.Config.LibraryRoot, App.Config.LibraryRoot, "{Title}").DictFormat(remoteMetadata);
            localFile = Utils.Files.MakeFilesystemSafe(localFile + Path.GetExtension(remoteEntry.FilePath));

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(localFile));
            }
            catch (Exception e)
            {
                Logger.Error(e, "Unable to create directories.");
                throw new Exception($"Unable to create directories; {e.Message}");
            }

            Database.BookEntry localEntry;
            if (File.Exists(localFile))
            {
                localEntry = LocalLibrary.FirstOrDefault(x => x.FilePath == localFile);
                if (localEntry == null) // target file exists but is *not* in local db
                {
                    Logger.Info("{} exists but is not in local database. File will be overwritten with remote copy.", localFile);
                    try
                    {
                        File.Delete(localFile);
                        File.Copy(remoteEntry.FilePath, localFile);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "Unable to overwrite file.");
                        throw new Exception($"Unable to overwrite file; {e.Message}");
                    }
                    try
                    {
                        App.LocalLibrary.ImportBook(remoteEntry);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "Unable to update library database.");
                        throw new Exception($"Unable to update library; {e.Message}");
                    }
                }
                else // target file exists and *is* in local db
                {
                    string msg = $"{localEntry.Title} exists in local library. Metadata will be copied from Kindle";
                    if (SelectedDevice != null && localEntry.Id != remoteEntry.Id)
                    {
                        Logger.Info(msg + " ID [{}] on {} will be changed from to [{}] to match local database.", remoteEntry.Id, SelectedDevice.Name, localEntry.Id);
                        try
                        {
                            SelectedDevice.Database.ChangeBookId(remoteEntry, localEntry.Id);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, "Unable to write to database.");
                            throw new Exception($"Unable to write to database; {e.Message}");
                        }
                    }
                    Logger.Info(msg, remoteEntry.Title);
                    try
                    {
                        localEntry.UpdateMetadata(remoteEntry);
                        App.LocalLibrary.Database.UpdateBook(localEntry);
                    }
                    catch (LiteDB.LiteException e)
                    {
                        Logger.Error(e, "Unable to write metadata to database.");
                        throw new Exception($"Unable to write metadata database; {e.Message}");
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "Unable to write metadata to disk.");
                        throw new Exception($"Unable to write metadata disk; {e.Message}");
                    }
                }
            }
            else
            {
                localEntry = LocalLibrary.FirstOrDefault(x => x.Id == remoteEntry.Id);
                if (localEntry != null)
                {

                    Logger.Info("{} found in database but not on disk, removing database entry before importing.", localEntry.Title);
                    try
                    {
                        App.LocalLibrary.Database.RemoveBook(localEntry);
                    }
                    catch (LiteDB.LiteException e)
                    {
                        Logger.Error(e, "Unable to write to database.");
                        throw new Exception($"Unable to write to database; {e.Message}");
                    }
                }

                try
                {
                    App.LocalLibrary.ImportBook(remoteEntry);
                }
                catch (LiteDB.LiteException e)
                {
                    Logger.Error(e, "Unable to write to database.");
                    throw new Exception($"Unable to write to database; {e.Message}");
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Unable to import book.");
                    throw new Exception($"Unable to import book; {e.Message}");
                }
            }
        }

        /// <summary>
        /// Used for importing from disk
        /// </summary>
        private void ImportBook(string filePath)
        {
            try
            {
                App.LocalLibrary.ImportBook(filePath);
            }
            catch (LiteDB.LiteException e)
            {
                throw new Exception($"Unable to write to database; {e.Message}");
            }
            catch (Exception e)
            {
                throw new Exception($"Import failed; {e.Message}");
            }
        }

        public async void ImportBooksDrop(string path)
        {
            if (File.Exists(path))
            {
                if (!Formats.Resources.AcceptedFileTypes.Contains(Path.GetExtension(path)))
                {
                    var errDlg = new Dialogs.Error("Incompatible Format", $"Importing {Path.GetExtension(path)} format books has not yet been implemented.");
                    _ = MaterialDesignThemes.Wpf.DialogHost.Show(errDlg);
                    return;
                }
                try
                {
                    ImportBook(path);
                }
                catch (Exception e)
                {
                    var dlg = new Dialogs.Error("Import failed", e.Message);
                    _ = MaterialDesignThemes.Wpf.DialogHost.Show(dlg);
                    return;
                }
            }
            else if (Directory.Exists(path))
            {
                var dlg = new Dialogs.BulkImport(path);
                await MaterialDesignThemes.Wpf.DialogHost.Show(dlg);
                if (dlg.DialogResult == false) return;

                string[] files = dlg.SelectedFiles();
                BackgroundWork = true;

                var prgDlg = new Dialogs.Progress("Importing books", false);
                int step = 100 / files.Length;

                _ = Task.Run(() =>
                {
                    List<Exception> errors = new List<Exception>();

                    foreach (string file in files)
                    {
                        StatusBarText = $"Importing {file}";
                        try
                        {
                            App.LocalLibrary.ImportBook(file);
                        }
                        catch (Exception e)
                        {
                            e.Data["item"] = file;
                            errors.Add(e);
                        }
                        finally
                        {
                            prgDlg.Percent += step;
                        }
                    }
                    prgDlg.Finish("Import finished.");

                    if (errors.Count > 0)
                    {
                        prgDlg.ShowError(new AggregateException(errors.ToArray()));
                    }
                });
            }
        }

        public void SaveLibraryColumns(List<string> hiddenColumns)
        {
            if (hiddenColumns.SequenceEqual(App.Config.HiddenColumns)) return;

            App.Config.HiddenColumns = hiddenColumns.ToArray();
            App.Config.Write();
        }
    }
}
