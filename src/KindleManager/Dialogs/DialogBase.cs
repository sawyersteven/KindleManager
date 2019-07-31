using System.Windows;
using System.Windows.Controls;

namespace KindleManager.Dialogs
{
    public class DialogBase : UserControl
    {
        public bool DialogResult = false;

        public void Close()
        {
            Close(null, null);
        }

        protected void Close(object sender, RoutedEventArgs e)
        {
            MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand.Execute(this, null);
        }

        protected virtual void Confirm(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close(sender, e);
        }

    }
}
