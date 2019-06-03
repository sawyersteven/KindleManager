using ReactiveUI;

namespace KindleManager.ViewModels
{
    class ErrorDialog : ReactiveObject
    {
        public ErrorDialog(string title, string message)
        {
            TitleText = title;
            MessageText = message;
        }

        public string TitleText { get; set; }
        public string MessageText { get; set; }
    }
}
