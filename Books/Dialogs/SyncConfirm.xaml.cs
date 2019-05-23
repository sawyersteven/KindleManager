using System.Windows;
using MahApps.Metro.Controls;
using System.Collections.Generic;
using Formats;
using Devices;
using ReactiveUI.Fody.Helpers;
using System.Linq;

namespace Books.Dialogs
{
    /// <summary>
    /// Interaction logic for Error.xaml
    /// </summary>
    public partial class SyncConfirm : MetroWindow
    {
        #region Fields      
        public UserSelectBook[] UserSelectedBooks { get; set; }
        public class UserSelectBook
        {
            [Reactive] public bool Checked { get; set; }
            [Reactive] public string Title { get; set; }
            public int Id { get; set; }
        }
        #endregion

        #region Props
        [Reactive] public string KindleName { get; set; }
        #endregion

        public SyncConfirm(List<Database.BookEntry> toTransfer, Device kindle)
        {
            UserSelectedBooks = new UserSelectBook[toTransfer.Count];
            for (int i = 0; i < toTransfer.Count; i++)
            {
                UserSelectedBooks[i] = new UserSelectBook { Checked = true, Title = toTransfer[i].Title, Id = toTransfer[i].Id };
            }

            UserSelectedBooks.OrderByDescending(x => x.Title);

            this.DataContext = this;
            this.Owner = App.Current.MainWindow;
            InitializeComponent();
            this.KindleName = kindle.Name;

            this.BooksListItemsControl.ItemsSource = UserSelectedBooks;
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void Confirm(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }
    }
}
