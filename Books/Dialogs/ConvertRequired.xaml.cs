using System.Windows;
using MahApps.Metro.Controls;

namespace Books.Dialogs
{
    public partial class ConvertRequired : MetroWindow 
    {
        public ConvertRequired(string title)
        {
            this.DataContext = this;
            this.Owner = App.Current.MainWindow;
            InitializeComponent();
            this.textBlockBookTitle.Text = title;
        }
        public bool DeleteFile { get; set; } = false;

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
