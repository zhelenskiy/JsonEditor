using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JsonEditor
{
    /// <summary>
    /// Окно для вставки новых элементов.
    /// </summary>
    public partial class PastingWindow : Window
    {
        public PastingWindow() => InitializeComponent();
        private INode Node { get; }
        private MainWindow Window { get; }

        internal PastingWindow(INode node, MainWindow window) : this()
        {
            Node = node;
            Window = window;
        }


        private void AcceptButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
        internal NameNode.PasteStatus PasteItem() => ChildButton.IsChecked == true ? PasteChild() : PasteNeighbor();
        /// <summary>
        /// Создание ребенка
        /// </summary>
        /// <remarks> 
        /// Текущая нода является родителем.
        /// </remarks>
        private NameNode.PasteStatus PasteChild() => Window.Copied.AddToINode(Node, null);
        /// <summary>
        /// Текущая нода является соседом
        /// </summary>
        private NameNode.PasteStatus PasteNeighbor()
        {
            var node = (NestedNode)Node;
            var parent = CommonMethods.GetSelectedTreeViewItemParent(Window, node);
            var currentIndex = parent.NamedSubItems().IndexOf(node.JsonObject) +
                               (AfterButton.IsChecked == true ? 1 : 0);
             return Window.Copied.AddToINode(parent, currentIndex);
        }

        private void PastingWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Node.NamedSubItems().Any())
            {
                ChildButton.IsEnabled = false;
            }
        }
    }
}
