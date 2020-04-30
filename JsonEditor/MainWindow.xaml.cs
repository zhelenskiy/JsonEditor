using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using JsonEditor.DataClasses;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace JsonEditor
{
    /// <summary>
    /// Logic of MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string JsonFilesFilter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
        private List<Station> _curStations;

        internal List<Station> СurStations
        {
            get => _curStations;
            set
            {
                var oldStations = _curStations;
                var oldTree = JsonTree;
                _curStations = value;
                try
                {
                    JsonTree.Items.Clear();
                    foreach (var station in СurStations)
                    {
                        BuildNode(JsonTree, station);
                    }
                }
                catch (Exception e)
                {
                    JsonTree = oldTree;
                    _curStations = oldStations;
                    throw new JsonEditorException("Invalid JSON file.", e);
                }
            }
        }

        private string _curFileName = "";

        private string CurFileName
        {
            get => _curFileName;
            set
            {
                _curFileName = value;
                Title = value;
            }
        }

        private JsonTreeViewItem Copied { get; set; }

        public MainWindow() => InitializeComponent();

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog {Filter = JsonFilesFilter};
            if (dlg.ShowDialog() != true) return;
            HandleException(() => ReadJson(dlg.FileName), "open the file");
        }

        internal void BuildNode(ItemsControl parent, INamed item)
        {
            var v = JsonTreeViewItemByINamed(item);
            foreach (var property in item.NamedSubItems())
            {
                BuildNode(v, property);
            }

            parent.Items.Add(v);
        }

        internal JsonTreeViewItem JsonTreeViewItemByINamed(INamed item)
        {
            var v = new JsonTreeViewItem {Header = item.name, JsonObject = item, ContextMenu = new ContextMenu()};
            var create = new JsonTreeViewMenuItem() {Header = "Create", Source = v};
            create.Click += Create_Click;
            v.ContextMenu.Items.Add(create);
            var rename = new JsonTreeViewMenuItem {Header = "Rename", Source = v};
            rename.Click += Rename_Click;
            v.ContextMenu.Items.Add(rename);
            var remove = new JsonTreeViewMenuItem {Header = "Remove", Source = v};
            remove.Click += Remove_Click;
            v.ContextMenu.Items.Add(remove);
            var copy = new JsonTreeViewMenuItem() {Header = "Copy", Source = v};
            copy.Click += Copy_Click;
            v.ContextMenu.Items.Add(copy);
            var paste = new JsonTreeViewMenuItem() {Header = "Paste", Source = v};
            paste.Click += Paste_Click;
            v.ContextMenu.Items.Add(paste);
            return v;
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            var jsonTreeViewMenuItem = sender as JsonTreeViewMenuItem;
            var createWindow = new CreationWindow(jsonTreeViewMenuItem?.Source, this);
            if (createWindow.ShowDialog() != true) return;
            createWindow.CreateItem();
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            HandleException(() =>
            {
                var selected = sender as JsonTreeViewMenuItem;
                AssertTypeEquality(Copied.JsonObject, selected?.Source.JsonObject);
            }, "paste the element due to the type discrepancy");
        }

        private void AssertTypeEquality(object copiedJsonObject, object sourceJsonObject)
        {
            if (copiedJsonObject.GetType() != sourceJsonObject.GetType())
            {
                throw new JsonEditorException(
                    "Couldn't match the type of the first object against the type of the second object.");
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            Copied = (sender as JsonTreeViewMenuItem)?.Source;
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            JsonTreeViewItem item = (sender as JsonTreeViewMenuItem)?.Source;
            if (item == null) return;
            if (GetSelectedTreeViewItemParent(item) is JsonTreeViewItem parent)
            {
                parent.Items.Remove(item);
                parent.JsonObject.RemoveSubItem(item.JsonObject);
            }
            else
            {
                JsonTree.Items.Remove(item);
                _curStations.Remove(item.JsonObject as Station);
            }
        }

        internal static ItemsControl GetSelectedTreeViewItemParent(DependencyObject item)
        {
            var parent = VisualTreeHelper.GetParent(item);
            while (!(parent is TreeViewItem || parent is TreeView || parent == null))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as ItemsControl;
        }

        private static void Rename_Click(object sender, RoutedEventArgs e)
        {
            var jsonTreeViewMenuItem = sender as JsonTreeViewMenuItem;
            var renameWindow = new RenamingWindow(jsonTreeViewMenuItem?.Source.JsonObject.name);
            if (renameWindow.ShowDialog() != true) return;
            Debug.Assert(jsonTreeViewMenuItem != null, nameof(jsonTreeViewMenuItem) + " != null");
            jsonTreeViewMenuItem.Source.JsonObject.name = renameWindow.ResultName;
            jsonTreeViewMenuItem.Source.Header = renameWindow.ResultName;
        }

        private void ReadJson(string path)
        {
            string data;
            try
            {
                data = File.ReadAllText(path);
            }
            catch (Exception exception)
            {
                throw new JsonEditorException("Cannot read this file.", exception);
            }

            try
            {
                СurStations = JsonConvert.DeserializeObject<List<Station>>(data);
            }
            catch (Exception exception)
            {
                throw new JsonEditorException("Invalid JSON file.", exception);
            }

            CurFileName = path;
        }

        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            HandleException(() =>
            {
                CheckDataToWrite();
                var dlg = new SaveFileDialog {Filter = JsonFilesFilter};
                if (dlg.ShowDialog() != true) return; //can be null
                WriteJson(dlg.FileName);
            }, "save as the new file");
        }

        private void CheckDataToWrite()
        {
            if (СurStations == null)
                throw new JsonEditorException("No opened files");
        }

        private void WriteJson(string path)
        {
            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(СurStations));
            }
            catch (Exception exception)
            {
                throw new JsonEditorException("Cannot write to this file.", exception);
            }

            CurFileName = path;
        }

        private static void HandleException(Action action, string actionName)
        {
            try
            {
                action.Invoke();
            }
            catch (JsonEditorException e)
            {
                MessageBox.Show($"{e.Message}\n{e.InnerException?.Message ?? ""}", $"Cannot {actionName}!");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            HandleException(() =>
            {
                CheckDataToWrite();
                WriteJson(CurFileName);
            }, "save the file");
        }

        private void JsonTree_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (СurStations != null && !СurStations.Any())
            {
                var menuItem = new MenuItem {Header = "Add the first Station"};
                var contextMenu = new ContextMenu();
                menuItem.Click += MenuItem_Click;
                contextMenu.Items.Add(menuItem);
                JsonTree.ContextMenu = contextMenu;
            }
            else
            {
                JsonTree.ContextMenu = null;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var createWindow = new CreationWindow(null, this);
            if (createWindow.ShowDialog() != true) return;
            createWindow.CreateItem();
        }
    }

    namespace DataClasses
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        internal interface INamed
        {
            string name { get; set; }

            INamed create(string _type, string _id, string _name);

            INamed createChild(string _type, string _id, string _name);

            void RemoveSubItem(INamed missing_subItem);

            void AddSubItem(INamed subItem);

            void AddSubItem(INamed subItem, int index);

            IEnumerable<INamed> NamedSubItems();
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        // ReSharper disable once ClassNeverInstantiated.Global
        internal class Station : INamed
        {
            public Station(string type, string id, string name)
            {
                this.type = type;
                this.id = id;
                this.name = name;
            }

            public string type { get; set; }
            public string id { get; set; }
            public string name { get; set; }

            public INamed create(string _type, string _id, string _name)
            {
                return new Station(_type, _id, _name);
            }

            public INamed createChild(string _type, string _id, string _name)
            {
                return new Arm(_type, _id, _name);
            }

            public void RemoveSubItem(INamed missing_subItem)
            {
                if (missing_subItem is Arm arm)
                {
                    items.Remove(arm);
                }
            }

            public void AddSubItem(INamed subItem)
            {
                if (subItem is Arm arm)
                {
                    items.Add(arm);
                }
            }

            public void AddSubItem(INamed subItem, int index)
            {
                if (subItem is Arm arm)
                {
                    items.Insert(index, arm);
                }
            }

            public IEnumerable<INamed> NamedSubItems() => items;

            public List<Arm> items { get; set; } = new List<Arm>();
        }


        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        // ReSharper disable once ClassNeverInstantiated.Global
        internal class Arm : INamed
        {
            public Arm(string type, string id, string name)
            {
                this.type = type;
                this.id = id;
                this.name = name;
            }

            public string type { get; set; }
            public string id { get; set; }
            public string name { get; set; }

            public INamed create(string _type, string _id, string _name)
            {
                return new Arm(_type, _id, _name);
            }

            public INamed createChild(string _type, string _id, string _name)
            {
                return new Device(_type, _id, _name);
            }

            public void RemoveSubItem(INamed missing_subItem)
            {
                if (missing_subItem is Device device)
                    items.Remove(device);
            }

            public void AddSubItem(INamed subItem)
            {
                if (subItem is Device device)
                {
                    items.Add(device);
                }
            }

            public void AddSubItem(INamed subItem, int index)
            {
                if (subItem is Device device)
                {
                    items.Insert(index, device);
                }
            }

            public IEnumerable<INamed> NamedSubItems() => items;

            public List<Device> items { get; set; } = new List<Device>();
        }


        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        // ReSharper disable once ClassNeverInstantiated.Global
        internal class Device : INamed
        {
            public Device(string type, string id, string name)
            {
                this.type = type;
                this.id = id;
                this.name = name;
            }

            public string type { get; set; }
            public string id { get; set; }
            public string name { get; set; }

            public INamed create(string _type, string _id, string _name)
            {
                return new Device(_type, _id, _name);
            }

            public INamed createChild(string missing_type, string missing_id, string missing_name)
            {
                return null;
            }

            public void RemoveSubItem(INamed missing_subItem)
            {
            }

            public void AddSubItem(INamed missing_subItem)
            {
            }

            public void AddSubItem(INamed missing_subItem, int missing_index)
            {
            }

            public IEnumerable<INamed> NamedSubItems() => Enumerable.Empty<INamed>();
        }
    }

    internal class JsonTreeViewItem : TreeViewItem
    {
        public INamed JsonObject { get; set; }
    }

    internal class JsonTreeViewMenuItem : MenuItem
    {
        public JsonTreeViewItem Source { get; set; }
    }
}