using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private const string DefaultTitle = "JSON editor";

        private string _curFileName;

        private string CurFileName
        {
            get => _curFileName;
            set
            {
                _curFileName = value;
                Title = value ?? DefaultTitle;
            }
        }

        internal RootNode Root { get; private set; }

        private string LastSaved { get; set; } = "";

        private NestedNode Copied { get; set; }
        internal ISet<string> UsedIds { get; set; } = new HashSet<string>();

        public MainWindow()
        {
            InitializeComponent();
            Root = new RootNode(this, new List<Station>());
            CurFileName = null;
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (NoUnsavedData())
            {
                var dlg = new OpenFileDialog {Filter = JsonFilesFilter};
                if (dlg.ShowDialog() != true) return;
                HandleException(() => ReadJson(dlg.FileName), "open the file");
            }
        }

        internal void Create_Click(object sender, RoutedEventArgs e)
        {
            HandleException((() =>
            {
                var jsonTreeViewMenuItem = (JsonTreeViewMenuItem) sender;
                var createWindow = new CreationWindow(jsonTreeViewMenuItem?.Source, this);
                if (createWindow.ShowDialog() != true) return;
                createWindow.CreateItem();
            }), "create such item!");
        }

        internal void Paste_Click(object sender, RoutedEventArgs e)
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

        internal void Copy_Click(object sender, RoutedEventArgs e)
        {
            Copied = (sender as JsonTreeViewMenuItem)?.Source;
        }

        internal void Remove_Click(object sender, RoutedEventArgs e)
        {
            NestedNode item = ((JsonTreeViewMenuItem) sender).Source;
            CommonMethods.GetSelectedTreeViewItemParent(this, item).RemoveSubItem(item);
        }

        internal static void Rename_Click(object sender, RoutedEventArgs e)
        {
            var nestedNode = ((JsonTreeViewMenuItem) sender)?.Source;
            Debug.Assert(nestedNode != null, nameof(nestedNode) + " != null");
            var renameWindow = new RenamingWindow(nestedNode.JsonObject.name);
            if (renameWindow.ShowDialog() != true) return;
            nestedNode.NodeName = renameWindow.ResultName;
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
                Root = new RootNode(this, JsonConvert.DeserializeObject<List<Station>>(data));
            }
            catch (Exception exception)
            {
                throw new JsonEditorException("Invalid JSON file.", exception);
            }

            LastSaved = data;
            CurFileName = path;
        }

        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            HandleException(() =>
            {
                var dlg = new SaveFileDialog {Filter = JsonFilesFilter};
                if (dlg.ShowDialog() != true) return; //can be null
                WriteJson(dlg.FileName);
            }, "save as the new file");
        }

        private void CheckDataToWrite()
        {
            if (CurFileName == null)
            {
                throw new JsonEditorException("No path to save to.");
            }
        }

        private void WriteJson(string path)
        {
            try
            {
                var contents = JsonConvert.SerializeObject(Root.Stations);
                File.WriteAllText(path, contents);
                LastSaved = contents;
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
            if (Root.Stations.Any())
            {
                JsonTree.ContextMenu = null;
            }
            else
            {
                var menuItem = new MenuItem {Header = "Add the first Station"};
                var contextMenu = new ContextMenu();
                menuItem.Click += RootNodeCreationButton_Click;
                contextMenu.Items.Add(menuItem);
                JsonTree.ContextMenu = contextMenu;
            }
        }

        private void RootNodeCreationButton_Click(object sender, RoutedEventArgs e)
        {
            var createWindow = new CreationWindow(Root, this);
            if (createWindow.ShowDialog() != true) return;
            UsedIds.Clear();
            createWindow.CreateItem();
        }

        private bool NoUnsavedData() =>
            LastSaved == "" && Root.Stations.Count == 0 || JsonConvert.SerializeObject(Root.Stations) == LastSaved ||
            MessageBox.Show("Are you sure that you want to discard the changes?", "Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes;

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (Root.Stations.Count == 0 && CurFileName == null && LastSaved == "")
            {
                MessageBox.Show("Empty file is already opened!", "Notification", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else if (NoUnsavedData())
            {
                CurFileName = null;
                Root = new RootNode(this, new List<Station>());
                LastSaved = "";
            }
        }
    }
}