using System.Windows;

namespace KindleManager.Dialogs
{
    public partial class YesNo
    {
        [ReactiveUI.Fody.Helpers.Reactive]
        public string Title { get; set; }

        public bool DialogResult = false;

        public YesNo(string title, string text, string yesButtonText = "OK")
        {
            this.DataContext = this;
            this.Title = title;
            InitializeComponent();
            this.BodyText.Text = text;
            this.YesButton.Text = yesButtonText;
        }

        public bool DeleteFile { get; set; } = false;

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
