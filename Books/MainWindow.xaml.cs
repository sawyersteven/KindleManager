using MahApps.Metro.Controls;

namespace Books
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            this.DataContext = new ViewModels.MainWindow();
            InitializeComponent();

        }
    }
}
