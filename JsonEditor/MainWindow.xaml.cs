using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        internal NameNode Copied { get; private set; }
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
            HandleException(() =>
            {
                var jsonTreeViewMenuItem = (JsonTreeViewMenuItem) sender;
                var creationWindow = new CreationWindow(jsonTreeViewMenuItem?.Source, this);
                if (creationWindow.ShowDialog() != true) return;
                creationWindow.CreateItem();
            }, "create such item!");
        }

        internal void Paste_Click(object sender, RoutedEventArgs e)
        {
            if (Copied == null)
            {
                MessageBox.Show("Nothing is copied!", "Can not paste", MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
            }
            else
            {
                var selected = (JsonTreeViewMenuItem) sender;
                var pastingWindow = new PastingWindow(selected.Source, this);
                if (pastingWindow.ShowDialog() != true) return;
                var caption = "Be careful";

                switch (pastingWindow.PasteItem())
                {
                    case NameNode.PasteStatus.PartiallyAdded:
                        MessageBox.Show("Copied fragment of JSON was only partially pasted!", caption,
                            MessageBoxButton.OK, MessageBoxImage.Asterisk);
                        break;
                    case NameNode.PasteStatus.NotAdded:
                        MessageBox.Show("Copied fragment was not pasted at all!", caption, MessageBoxButton.OK,
                            MessageBoxImage.Asterisk);
                        break;
                }
            }
        }

        internal void Copy_Click(object sender, RoutedEventArgs e) =>
            Copied = new NameNode(((JsonTreeViewMenuItem) sender).Source.JsonObject, this);

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
                MessageBox.Show($"{e.Message}\n{e.InnerException?.Message ?? ""}", $"Cannot {actionName}!",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                var contextMenu = new ContextMenu();
                var addMenuItem = new MenuItem {Header = "Add the first Station"};
                addMenuItem.Click += RootNodeCreationButton_Click;
                contextMenu.Items.Add(addMenuItem);
                if (Copied != null)
                {
                    var pasteMenuItem = new MenuItem {Header = "Paste"};
                    pasteMenuItem.Click += (o, args) => Copied.AddToINode(Root, null);
                    contextMenu.Items.Add(pasteMenuItem);
                }

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

        private void JsonTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is NestedNode node)
            {
                CurrentId.Text = node.JsonObject.id;
                CurrentName.Text = node.JsonObject.name;
                CurrentType.Text = node.JsonObject.type;
            }
            else
            {
                CurrentId.Text = CurrentName.Text = CurrentType.Text = "";
            }
        }
    }
}