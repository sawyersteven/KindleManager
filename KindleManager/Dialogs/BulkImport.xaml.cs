using MahApps.Metro.Controls;
using ReactiveUI.Fody.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace KindleManager.Dialogs
{
    /// <summary>
    /// Interaction logic for Error.xaml
    /// </summary>
    public partial class BulkImport : MetroWindow
    {
        public List<TreeNode> Tree { get; set; }

        public BulkImport(string dir)
        {
            this.DataContext = this;
            this.Owner = App.Current.MainWindow;

            TreeNode root = new TreeNode();

            ReadDirTree(dir, root);
            Tree = root.Children;

            InitializeComponent();
        }

        private void ReadDirTree(string dir, TreeNode node)
        {
            foreach (string filepath in Directory.GetFiles(dir))
            {
                if (Formats.Resources.AcceptedFileTypes.Contains(Path.GetExtension(filepath)))
                {
                    node.Children.Add(new TreeNode { Name = Path.GetFileName(filepath), Fullname = filepath });
                }
            }

            foreach (string dirpath in Directory.GetDirectories(dir))
            {
                TreeNode child = new TreeNode { Name = new DirectoryInfo(dirpath).Name, IsFile = false };
                ReadDirTree(dirpath, child);
                if (child.Children.Count > 0)
                {
                    node.Children.Add(child);
                }

            }
        }

        public class TreeNode
        {
            public string Name { get; set; }
            public string Fullname { get; set; }
            public List<TreeNode> Children { get; set; }
            public bool IsFile { get; set; }
            [Reactive] public bool Checked { get; set; }

            public TreeNode()
            {
                Name = "";
                Checked = true;
                IsFile = true;
                Children = new List<TreeNode>();
            }
        }

        public string[] SelectedFiles()
        {
            TreeNode root = new TreeNode { IsFile = false };
            root.Children = Tree;
            return GetSelectedFiles(root);
        }

        /// <summary>
        /// Recursively gets all selected files in node and children
        /// </summary>
        private string[] GetSelectedFiles(TreeNode node)
        {
            List<string> files = new List<string>();

            foreach (TreeNode child in node.Children)
            {
                if (child.IsFile && child.Checked)
                {
                    files.Add(child.Fullname);
                }
                else
                {
                    files.AddRange(GetSelectedFiles(child));
                }
            }
            return files.ToArray();
        }

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
