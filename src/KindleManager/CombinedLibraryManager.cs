using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;

namespace KindleManager
{
    class CombinedLibraryManager : ObservableCollection<LibraryEntry>
    {
        public object Lock = new object();

        public CombinedLibraryManager(ObservableCollection<Database.BookEntry> localLibrary)
        {
            foreach (Database.BookEntry i in localLibrary)
            {
                this.Add(new LibraryEntry(i) { IsLocal = true });
            }

            localLibrary.CollectionChanged += LocalCollectionChanged;

            BindingOperations.EnableCollectionSynchronization(this, Lock);
        }

        /// <summary>
        /// Replaces current RemoteLibrary books and adds event hook to maintain parity
        /// </summary>
        public void AddRemoteLibrary(ObservableCollection<Database.BookEntry> remoteLibrary)
        {
            foreach (LibraryEntry i in this)
            {
                if (i.IsLocal)
                {
                    i.IsRemote = false;
                }
                else
                {
                    this.Remove(i);
                }
            }

            foreach (Database.BookEntry i in remoteLibrary)
            {

                LibraryEntry local = this.FirstOrDefault(x => x.Id == i.Id);
                if (local != null)
                {
                    local.IsRemote = true;
                }
                else
                {
                    this.Add(new LibraryEntry(i) { IsRemote = true });
                }
            }
            remoteLibrary.CollectionChanged += RemoteCollectionChanged;
        }

        public void RemoveRemoteLibrary(ObservableCollection<Database.BookEntry> remoteLibrary)
        {
            remoteLibrary.CollectionChanged -= RemoteCollectionChanged;
            foreach (LibraryEntry i in this)
            {
                if (i.IsLocal)
                {
                    i.IsRemote = false;
                }
                else
                {
                    this.Remove(i);
                }
            }
        }


        private void RemoteCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (Database.BookEntry i in e.NewItems)
                    {
                        var existing = this.FirstOrDefault(x => x.Id == i.Id);
                        if (existing == null)
                        {
                            this.Add(new LibraryEntry(i) { IsRemote = true });
                        }
                        else
                        {
                            existing.IsLocal = true;
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (Database.BookEntry i in e.OldItems)
                    {
                        var existing = this.FirstOrDefault(x => x.Id == i.Id);
                        if (existing == null) continue;

                        if (existing.IsLocal)
                        {
                            existing.IsRemote = false;
                        }
                        else
                        {
                            this.Remove(existing);
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    foreach (Database.BookEntry i in e.NewItems)
                    {
                        if (this.FirstOrDefault(x => x.Id == i.Id) is Database.BookEntry existing)
                        {
                            existing.CopyFrom(i);
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        public void LocalCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (Database.BookEntry i in e.NewItems)
                    {
                        var existing = this.FirstOrDefault(x => x.Id == i.Id);
                        if (existing == null)
                        {
                            this.Add(new LibraryEntry(i) { IsLocal = true });
                        }
                        else
                        {
                            existing.IsLocal = true;
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (Database.BookEntry i in e.OldItems)
                    {
                        var existing = this.FirstOrDefault(x => x.Id == i.Id);
                        if (existing == null) continue;

                        if (existing.IsRemote)
                        {
                            existing.IsLocal = false;
                        }
                        else
                        {
                            this.Remove(existing);
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    foreach (Database.BookEntry i in e.NewItems)
                    {
                        if (this.FirstOrDefault(x => x.Id == i.Id) is Database.BookEntry existing)
                        {
                            existing.CopyFrom(i);
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    break;
            }
        }
    }

    public class LibraryEntry : Database.BookEntry
    {
        [Reactive] public bool IsLocal { get; set; } = false;
        [Reactive] public bool IsRemote { get; set; } = false;

        public LibraryEntry(Database.BookEntry dbEntry)
        {
            this.Id = dbEntry.Id;

            this.FilePath = dbEntry.FilePath;

            this.Title = dbEntry.Title;
            this.Language = dbEntry.Language;
            this.ISBN = dbEntry.ISBN;

            this.Author = dbEntry.Author;
            this.Contributor = dbEntry.Contributor;
            this.Publisher = dbEntry.Publisher;
            this.Subject = dbEntry.Subject;
            this.Description = dbEntry.Description;
            this.PubDate = dbEntry.PubDate;
            this.Rights = dbEntry.Rights;

            this.Series = dbEntry.Series;
            this.SeriesNum = dbEntry.SeriesNum;
            this.DateAdded = dbEntry.DateAdded;
        }
    }
}
