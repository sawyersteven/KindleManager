using System;
using System.Windows;

namespace KindleManager.Dialogs
{
    public partial class Progress
    {
        #region props
        private int _Percent = 0;
        public int Percent
        {
            get => _Percent;
            set
            {
                _Percent = value;
                App.Current.Dispatcher.Invoke(() => progressBar.Value = value);
            }
        }

        public string _Current = "";
        public string Current
        {
            get => _Current;
            set
            {
                App.Current.Dispatcher.Invoke(() => currentText.Text = value);
            }
        }

        public string Title { get; set; }
        public string BodyText { get; set; }
        #endregion

        public Progress(string Title, bool IsIndeterminate)
        {
            this.DataContext = this;
            this.Title = Title;
            InitializeComponent();
            progressBar.IsIndeterminate = IsIndeterminate;
        }

        public void ShowError(Exception e)
        {
            if (e is AggregateException errs)
            {
                GridRow[] rows = new GridRow[errs.InnerExceptions.Count];

                for (int i = 0; i < rows.Length; i++)
                {
                    rows[i] = new GridRow((string)errs.InnerExceptions[i].Data["item"], errs.InnerExceptions[i].Message);
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    errorsTable.ItemsSource = rows;
                    errorsView.Visibility = Visibility.Visible;
                });
            }
            else
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    singleErrorText.Text = e.Message;
                    singleErrorView.Visibility = Visibility.Visible;
                });
            }
        }

        public void Finish(string msg = "")
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Current = msg;
                progressBar.IsIndeterminate = false;
                progressBar.Value = 100;
                closeButton.IsEnabled = true;
            });
        }

        public void Close(object sender, RoutedEventArgs e)
        {
            MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand.Execute(this, null);
        }

        private class GridRow
        {
            public string Item { get; set; }
            public string Error { get; set; }
            public GridRow(string f, string e)
            {
                Item = f;
                Error = e;
            }
        }
    }
}
