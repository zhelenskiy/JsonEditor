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
    /// Основное окно, которое содержит графическое представление нашего JSON-файла
    /// </summary>
    /// <remarks>
    /// Root представляет из себя основную корневую ноду
    /// </remarks>
    public partial class MainWindow : Window
    {
        private const string JsonFilesFilter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
        private const string DefaultTitle = "JSON editor";

        private string _curFileName;

        /// <summary>
        /// Отвечает за путь к открытому файлу
        /// </summary>
        private string CurFileName
        {
            get => _curFileName;
            set
            {
                _curFileName = value;
                Title = value ?? DefaultTitle;
            }
        }

        /// <summary>
        /// Основная корневая нода
        /// </summary>
        internal RootNode Root { get; private set; }
        /// <summary>
        /// Последнее сохраненное состояние
        /// </summary>
        private string LastSaved { get; set; } = "";
        /// <summary>
        /// Скопированный объект
        /// </summary>
        internal NameNode Copied { get; private set; }
        /// <summary>
        /// ИСпользованное в данном файле ID
        /// </summary>
        internal ISet<string> UsedIds { get; set; } = new HashSet<string>();

        public MainWindow()
        {
            InitializeComponent();
            Root = new RootNode(this, new List<Station>());
            CurFileName = null;
        }

        /// <summary>
        /// Событие происходящее при нажати кнопки откытия файла
        /// </summary>
        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (NoUnsavedData())
            {
                var dlg = new OpenFileDialog {Filter = JsonFilesFilter};
                if (dlg.ShowDialog() != true) return;
                HandleException(() => ReadJson(dlg.FileName), "open the file");
            }
        }
        /// <summary>
        /// Создание элемента в структуре json-файла
        /// </summary>
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
        /// <summary>
        /// Вставка из буффера части JSON файла
        /// </summary>
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
        /// <summary>
        /// Копирование в буффер части JSON файла
        /// </summary>
        internal void Copy_Click(object sender, RoutedEventArgs e) =>
            Copied = new NameNode(((JsonTreeViewMenuItem) sender).Source.JsonObject, this);
        /// <summary>
        /// Удаление части JSON файла
        /// </summary>
        internal void Remove_Click(object sender, RoutedEventArgs e)
        {
            NestedNode item = ((JsonTreeViewMenuItem) sender).Source;
            CommonMethods.GetSelectedTreeViewItemParent(this, item).RemoveSubItem(item);
        }
        /// <summary>
        /// Переименновывает элементы JSON-файла
        /// </summary>
        /// 
        internal static void Rename_Click(object sender, RoutedEventArgs e)
        {
            var nestedNode = ((JsonTreeViewMenuItem) sender)?.Source;
            Debug.Assert(nestedNode != null, nameof(nestedNode) + " != null");
            var renameWindow = new RenamingWindow(nestedNode.JsonObject.name);
            if (renameWindow.ShowDialog() != true) return;
            nestedNode.NodeName = renameWindow.ResultName;
        }
        /// <summary>
        /// Читает из файла.
        /// </summary>
        /// <remarks>
        /// Строгая гарантия безопасности.
        /// </remarks>
        /// /// <exception cref="JsonEditorException"> Если не получается загрузить json файл</exception>
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
        /// <summary>
        /// Save as the new file.
        /// </summary>
        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            HandleException(() =>
            {
                var dlg = new SaveFileDialog {Filter = JsonFilesFilter};
                if (dlg.ShowDialog() != true) return; //can be null
                WriteJson(dlg.FileName);
            }, "save as the new file");
        }
        /// <summary>
        /// Проверяет есть ли путь для записи файла
        /// </summary>
        private void CheckDataToWrite()
        {
            if (CurFileName == null)
            {
                throw new JsonEditorException("No path to save to.");
            }
        }
        /// <summary>
        /// Записывает текущий JSON файл
        /// </summary>
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
        /// <summary>
        /// Уведомляет пользователя об ошибке произошедшей из-за неверных данных
        /// </summary>
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
        /// <summary>
        /// Сохраняет файл по посленему сохраненному пути
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            HandleException(() =>
            {
                CheckDataToWrite();
                WriteJson(CurFileName);
            }, "save the file");
        }
        /// <summary>
        /// Обрабатывает попытку вызвать контекстное меню у всего дерева
        /// </summary>
        /// <remarks>
        /// Если дерево пустое, то мы можем создать первый элемент или вставить элемент из буфера
        /// </remarks>
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
         /// <summary>
         /// Вырезает элемент json файла
         /// </summary>
        internal void Cut_Click(object sender, RoutedEventArgs args)
        {
            Copy_Click(sender, args);
            Remove_Click(sender, args);
        }
        /// <summary>
        /// Создание первой ноды
        /// </summary>
        private void RootNodeCreationButton_Click(object sender, RoutedEventArgs e)
        {
            var createWindow = new CreationWindow(Root, this);
            if (createWindow.ShowDialog() != true) return;
            UsedIds.Clear();
            createWindow.CreateItem();
        }
        /// <summary>
        /// проверяет что у нас нет несохраненных данных, которые мы можем потерять при замене на другое
        /// </summary>
        private bool NoUnsavedData() =>
            LastSaved == "" && Root.Stations.Count == 0 || JsonConvert.SerializeObject(Root.Stations) == LastSaved ||
            MessageBox.Show("Are you sure that you want to discard the changes?", "Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes;
        /// <summary>
        /// Обработка создания нового файла
        /// </summary>
        private void NewButton_OnClick(object sender, RoutedEventArgs e)
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
        /// <summary>
        /// Поддерживает в согласованном состоянии показ свойств выделенного элемента
        /// </summary>
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
        /// <summary>
        /// Обработка горячих клавиш.
        /// </summary>
        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            {
                switch (e.Key)
                {
                    case Key.N:
                        NewButton_OnClick(NewButton, e);
                        break;
                    case Key.O:
                        OpenButton_Click(OpenButton, e);
                        break;
                    case Key.S when e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift):
                        SaveAsButton_Click(SaveAsButton, e);
                        break;
                    case Key.S:
                        SaveButton_Click(SaveButton, e);
                        break;
                }

                var contextMenuItems = (JsonTree.SelectedItem as NestedNode)?.ContextMenu?.Items
                    .Cast<JsonTreeViewMenuItem>().ToDictionary(t => t.Operation, t => t);
                if (contextMenuItems != null)
                {
                    switch (e.Key)
                    {
                        case Key.C:
                            Copy_Click(contextMenuItems[JsonTreeViewMenuItem.OperationType.Copy], e);
                            break;
                        case Key.V:
                            Paste_Click(contextMenuItems[JsonTreeViewMenuItem.OperationType.Paste], e);
                            break;
                        case Key.X:
                            Cut_Click(contextMenuItems[JsonTreeViewMenuItem.OperationType.Cut], e);
                            break;
                    }
                }
            }
        }
    }
}