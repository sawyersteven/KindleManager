namespace KindleManager.Dialogs
{
    public partial class YesNo : DialogBase
    {
        [ReactiveUI.Fody.Helpers.Reactive]
        public string Title { get; set; }
        public bool DeleteFile { get; set; } = false;

        public YesNo(string title, string text, string yesButtonText = "OK")
        {
            this.DataContext = this;
            this.Title = title;
            InitializeComponent();
            this.BodyText.Text = text;
            this.YesButton.Text = yesButtonText;
        }
    }
}
