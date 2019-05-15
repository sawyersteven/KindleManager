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
        private List<BookBase> ToTransfer;
        private UserSelectBook[] UserSelectedBooks { get; set; }
        public struct UserSelectBook
        {
            [Reactive] public bool Checked { get; set; }
            [Reactive] public string Title { get; set; }
        }
        #endregion

        #region Props
        [Reactive] public string KindleName { get; set; }
        #endregion

        public SyncConfirm(List<BookBase> toTransfer, Device kindle)
        {
            UserSelectedBooks = new UserSelectBook[toTransfer.Count * 3];
            for (int i = 0; i < toTransfer.Count; i++)
            {
                UserSelectedBooks[i] = new UserSelectBook { Checked = true, Title = toTransfer[i].Title };
            }
            for (int i = toTransfer.Count; i < toTransfer.Count * 2; i++)
            {
                UserSelectedBooks[i] = new UserSelectBook { Checked = true, Title = toTransfer[i - toTransfer.Count].Title };
            }
            for (int i = toTransfer.Count * 2; i < toTransfer.Count * 3; i++)
            {
                UserSelectedBooks[i] = new UserSelectBook { Checked = true, Title = toTransfer[i - (toTransfer.Count * 2)].Title };
            }

            UserSelectedBooks.OrderByDescending(x => x.Title);

            this.DataContext = this;
            this.Owner = App.Current.MainWindow;
            InitializeComponent();
            this.ToTransfer = toTransfer;
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
