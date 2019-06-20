using MahApps.Metro.Controls;
using System;
using System.Windows;

using GridRow = System.ValueTuple<string, string>;

namespace KindleManager.Dialogs
{
    /// <summary>
    /// Interaction logic for Error.xaml
    /// </summary>
    public partial class BulkProcessErrors : MetroWindow
    {
        private GridRow[] Errors { get; set; }

        public BulkProcessErrors(string message, Exception[] errors)
        {
            Errors = new GridRow[errors.Length];

            for (int i = 0; i < errors.Length; i++)
            {
                Errors[i] = ((string)errors[i].Data["File"], errors[i].Message);
            }

            this.DataContext = this;
            this.Owner = App.Current.MainWindow;

            InitializeComponent();
            Message.Text = message;
            ErrorTable.ItemsSource = Errors;
        }

        //private struct GridRow //TODO can be tuple?
        //{
        //    public string File { get; set; }
        //    public string Message { get; set; }
        //}

        private void Close(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
