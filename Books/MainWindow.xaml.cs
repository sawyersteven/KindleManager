using System.Windows;
using System.Windows.Input;
using System;

namespace Books
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.DataContext = new ViewModels.MainWindow();
            InitializeComponent();
        }
    }
}
