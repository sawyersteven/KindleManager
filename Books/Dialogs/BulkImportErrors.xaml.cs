using System.Windows;
using MahApps.Metro.Controls;
using System.IO;
using ReactiveUI.Fody.Helpers;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Books.Dialogs
{
    /// <summary>
    /// Interaction logic for Error.xaml
    /// </summary>
    public partial class BulkImportErrors : MetroWindow
    {
        private GridRow[] Errors { get; set; }

        public BulkImportErrors((string, string)[] errors)
        {
            Errors = new GridRow[errors.Length];

            for (int i = 0; i < errors.Length; i++)
            {
                Errors[i] = new GridRow { File = errors[i].Item1, Message = errors[i].Item2 };
            }
            
            this.DataContext = this;
            this.Owner = App.Current.MainWindow;

            InitializeComponent();
            ErrorTable.ItemsSource = Errors;
        }

        private struct GridRow
        {
            public string File { get; set; }
            public string Message { get; set; }
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
