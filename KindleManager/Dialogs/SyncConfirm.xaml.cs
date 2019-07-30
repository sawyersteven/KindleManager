using ReactiveUI.Fody.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace KindleManager.Dialogs
{
    /// <summary>
    /// Interaction logic for Error.xaml
    /// </summary>
    public partial class SyncConfirm
    {
        #region Fields      
        public UserSelectBook[] UserSelectedBooks { get; set; }
        public class UserSelectBook
        {
            [Reactive] public bool Checked { get; set; }
            [Reactive] public string Title { get; set; }
            public int Id { get; set; }
        }
        public bool DialogResult = false;
        #endregion

        #region Props
        [Reactive] public string KindleName { get; set; }
        #endregion

        public SyncConfirm(List<Database.BookEntry> toTransfer, string kindleName)
        {
            UserSelectedBooks = new UserSelectBook[toTransfer.Count];
            for (int i = 0; i < toTransfer.Count; i++)
            {
                UserSelectedBooks[i] = new UserSelectBook { Checked = true, Title = toTransfer[i].Title, Id = toTransfer[i].Id };
            }

            UserSelectedBooks.OrderByDescending(x => x.Title);

            this.DataContext = this;
            this.KindleName = kindleName;
            InitializeComponent();


            this.BooksListItemsControl.ItemsSource = UserSelectedBooks;
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand.Execute(this, null);
        }

        private void Confirm(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close(sender, e);
        }
    }
}
