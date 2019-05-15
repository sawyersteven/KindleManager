using System.Windows;
using MahApps.Metro.Controls;

namespace Books.Dialogs
{
    public partial class YesNo : MetroWindow 
    {
        public YesNo(string title, string text, string yesButtonText = "OK")
        {
            this.DataContext = this;
            this.Owner = App.Current.MainWindow;
            InitializeComponent();
            this.Title = title;
            this.BodyText.Text = text;
            this.YesButton.Text = yesButtonText;

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
