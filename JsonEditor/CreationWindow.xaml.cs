using JsonEditor.DataClasses;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace JsonEditor
{
    /// <summary>
    /// Interaction logic for CreationWindow.xaml
    /// </summary>
    public partial class CreationWindow : Window
    {
        private INode Node { get; }
        private MainWindow Window { get; }

        public CreationWindow()
        {
            InitializeComponent();
        }

        internal CreationWindow(INode node, MainWindow window) : this()
        {
            Node = node;
            Window = window;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CreationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Node is RootNode)
            {
                ChildButton.IsChecked = true;
                BeforeButton.IsEnabled = false;
                AfterButton.IsEnabled = false;
            }
            else if (Node is NestedNode nested && nested.JsonObject is Device || Node.NamedSubItems().Any())
            {
                ChildButton.IsEnabled = false;
            }
        }

        internal void CreateItem()
        {
            if (ChildButton.IsChecked == true)
            {
                CreateChild();
            }
            else
            {
                CreateNeighbor();
            }
        }

        private void CreateChild()
        {
            var childJsonObject = Node.CreateChild(NewType.Text, NewId.Text, NewName.Text);
            Node.AddSubItem(childJsonObject, null);
        }

        private void CreateNeighbor()
        {
            var node = (NestedNode) Node;
            var parent = CommonMethods.GetSelectedTreeViewItemParent(Window, node);
            var currentIndex = parent.NamedSubItems().IndexOf(node.JsonObject) +
                               (AfterButton.IsChecked == true ? 1 : 0);
            var createdNeighborJsonObject = parent.CreateChild(NewType.Text, NewId.Text, NewName.Text);
            parent.AddSubItem(createdNeighborJsonObject, currentIndex);
        }
    }
}