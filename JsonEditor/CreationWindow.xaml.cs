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
        private JsonTreeViewItem _item;
        private MainWindow _mainWindow;

        public CreationWindow()
        {
            InitializeComponent();
        }

        internal CreationWindow(JsonTreeViewItem item, MainWindow mainWindow) : this()
        {
            _item = item;
            _mainWindow = mainWindow;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CreationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_item == null)
            {
                ChildButton.IsChecked = true;
                BeforeButton.IsEnabled = false;
                AfterButton.IsEnabled = false;
            }
            else if (_item.JsonObject is Device || _item.JsonObject.NamedSubItems().Any())
            {
                ChildButton.IsEnabled = false;
            }
        }

        internal void CreateItem()
        {
            if (_item == null)
            {
                _mainWindow.СurStations = new List<Station> {new Station(NewType.Text, NewId.Text, NewName.Text)};
            }
            else if (ChildButton.IsChecked == true)
            {
                var childJsonObject = _item.JsonObject.createChild(NewType.Text, NewId.Text, NewName.Text);
                _item.JsonObject.AddSubItem(childJsonObject);
                _mainWindow.BuildNode(_item, childJsonObject);
            }
            else
            {
                var parent = MainWindow.GetSelectedTreeViewItemParent(_item);
                var currentIndex = parent.Items.IndexOf(_item) + (AfterButton.IsChecked == true ? 1 : 0);
                if (parent is JsonTreeViewItem parentJsonTreeViewItem)
                {
                    var neighborJsonObject = parentJsonTreeViewItem
                        .JsonObject
                        .createChild(NewType.Text, NewId.Text, NewName.Text);
                    parentJsonTreeViewItem.JsonObject.AddSubItem(neighborJsonObject, currentIndex);
                    parentJsonTreeViewItem.Items.Insert(currentIndex,
                        _mainWindow.JsonTreeViewItemByINamed(neighborJsonObject));
                }
                else
                {
                    var neighborJsonObject = new Station(NewType.Text, NewId.Text, NewName.Text);
                    _mainWindow.СurStations.Insert(currentIndex, neighborJsonObject);
                    ((TreeView) parent).Items.Insert(currentIndex,
                        _mainWindow.JsonTreeViewItemByINamed(neighborJsonObject));
                }
            }
        }
    }
}