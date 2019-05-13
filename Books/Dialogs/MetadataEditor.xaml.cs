using System.Windows;
using System;
using MahApps.Metro.Controls;

namespace Books.Dialogs
{
    public partial class MetadataEditor : MetroWindow
    {
        public MetadataEditor(Formats.IBook book)
        {
            ViewModels.MetadataEditor dc = new ViewModels.MetadataEditor(book);
            dc.CloseDialog = new Action(() => this.Close());
            this.DataContext = dc;

            InitializeComponent();

        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this.MinWidth = this.ActualWidth;
            this.MinHeight = this.ActualHeight;
            this.MaxHeight = this.ActualHeight;
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
