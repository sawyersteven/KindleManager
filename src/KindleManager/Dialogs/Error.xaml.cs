namespace KindleManager.Dialogs
{
    /// <summary>
    /// Interaction logic for Error.xaml
    /// </summary>
    public partial class Error : DialogBase
    {
        public string TitleText { get; set; }
        public string MessageText { get; set; }

        public Error(string title, string message)
        {
            TitleText = title;
            MessageText = message;
            this.DataContext = this;
            InitializeComponent();
        }
    }
}
