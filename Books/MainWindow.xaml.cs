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

            Library.DragEnter += Library_DragEnter;
            Library.Drop += Library_Drop;

        }

        private void Library_Drop(object sender, System.Windows.DragEventArgs e)
        {
            ViewModels.MainWindow vm = (ViewModels.MainWindow)DataContext;
         //   this.DataContext
        }

        private void Library_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            //throw new System.NotImplementedException();
        }
    }
}
