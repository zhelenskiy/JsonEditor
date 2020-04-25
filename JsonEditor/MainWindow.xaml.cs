using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

        private List<Station> СurStations
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

        public MainWindow() => InitializeComponent();

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog {Filter = JsonFilesFilter};
            if (dlg.ShowDialog() != true) return;
            HandleException(() => ReadJson(dlg.FileName), "open the file");
        }

        private void BuildNode(ItemsControl parent, INamed item)
        {
            var v = new JsonTreeViewItem {Header = item.name, JsonObject = item, ContextMenu = new ContextMenu()};
            var rename = new JsonTreeViewMenuItem {Header = "Rename", Source = v};
            rename.Click += Rename_Click;
            v.ContextMenu.Items.Add(rename);
            var remove = new JsonTreeViewMenuItem {Header = "Remove", Source = v};
            remove.Click += Remove_Click;
            v.ContextMenu.Items.Add(remove);
            foreach (INamed property in item.NamedItems)
            {
                BuildNode(v, property);
            }

            parent.Items.Add(v);
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            JsonTreeViewItem item = (sender as JsonTreeViewMenuItem)?.Source;
            if (item == null) return;
            if (GetSelectedTreeViewItemParent(item) is JsonTreeViewItem parent)
            {
                parent.Items.Remove(item);
                parent.JsonObject.Remove(item.JsonObject);
            }
            else
            {
                JsonTree.Items.Remove(item);
                _curStations.Remove(item.JsonObject as Station);
            }
        }

        private static ItemsControl GetSelectedTreeViewItemParent(DependencyObject item)
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
            var renameWindow = new RenameWindow(jsonTreeViewMenuItem?.Source.JsonObject.name);
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
                throw new JsonEditorException("Can not read this file.", exception);
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
                throw new JsonEditorException("Can not write to this file.", exception);
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
                MessageBox.Show($"{e.Message}\n{e.InnerException?.Message ?? ""}", $"Can not {actionName}!");
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
    }

    namespace DataClasses
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        internal interface INamed
        {
            string name { get; set; }
            void Remove(INamed subItem);

            IEnumerable<INamed> NamedItems { get; }
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        // ReSharper disable once ClassNeverInstantiated.Global
        internal class Station : INamed
        {
            public string type { get; set; }
            public string id { get; set; }
            public string name { get; set; }

            public void Remove(INamed subItem)
            {
                if (subItem is Arm arm)
                    items.Remove(arm);
            }

            public IEnumerable<INamed> NamedItems => items;

            public List<Arm> items { get; set; }
        }


        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        // ReSharper disable once ClassNeverInstantiated.Global
        internal class Arm : INamed
        {
            public string type { get; set; }
            public string id { get; set; }
            public string name { get; set; }

            public void Remove(INamed subItem)
            {
                if (subItem is Device device)
                    items.Remove(device);
            }

            public IEnumerable<INamed> NamedItems => items;

            public List<Device> items { get; set; }
        }


        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        // ReSharper disable once ClassNeverInstantiated.Global
        internal class Device : INamed
        {
            public string type { get; set; }
            public string id { get; set; }
            public string name { get; set; }

            public void Remove(INamed missing_name)
            {
            }

            public IEnumerable<INamed> NamedItems => Enumerable.Empty<INamed>();
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