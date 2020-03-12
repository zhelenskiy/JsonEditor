using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using JsonEditor.DataClasses;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace JsonEditor
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string JsonFilesFilter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
        private Station[] _curStations;

        private Station[] СurStations
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

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenButtonClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog {Filter = JsonFilesFilter};
            if (dlg.ShowDialog() != true) return;
            HandleException(() => ReadJson(dlg.FileName), "open the file");
        }

        private static void BuildNode(ItemsControl cur, INamed item)
        {
            var v = new JsonTreeViewItem {Header = GetProperty(item, "name"), JsonObject = item};
            foreach (INamed property in (IEnumerable) GetProperty(item, "items") ?? new ArrayList())
            {
                BuildNode(v, property);
            }

            cur.Items.Add(v);
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
                СurStations = JsonConvert.DeserializeObject<Station[]>(data);
                CurFileName = path;
            }
            catch (Exception exception)
            {
                throw new JsonEditorException("Invalid JSON file.", exception);
            }
        }

        private void SaveAsButtonClick(object sender, RoutedEventArgs e)
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
                CurFileName = path;
            }
            catch (Exception exception)
            {
                throw new JsonEditorException("Can not write to this file.", exception);
            }
        }

        private static void HandleException(Action action, string actionName)
        {
            try
            {
                action.Invoke();
            }
            catch (JsonEditorException e)
            {
                MessageBox.Show(e.Message, $"Can not {actionName}!");
            }
        }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            HandleException(() =>
            {
                CheckDataToWrite();
                WriteJson(CurFileName);
            }, "save the file");
        }

        private static object GetProperty(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName)?.GetValue(obj);
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
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        internal class Station : INamed
        {
            public string type { get; set; }
            public string id { get; set; }
            public string name { get; set; }
            public Arm[] items { get; set; }
        }


        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        internal class Arm : INamed
        {
            public string type { get; set; }
            public string id { get; set; }
            public string name { get; set; }
            public Device[] items { get; set; }
        }


        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        internal class Device : INamed
        {
            public string type { get; set; }
            public string id { get; set; }
            public string name { get; set; }
        }
    }

    internal class JsonTreeViewItem : TreeViewItem
    {
        public INamed JsonObject { get; set; }
    }

    internal class JsonEditorException : Exception
    {
        public JsonEditorException()
        {
        }

        public JsonEditorException(string message) : base(message)
        {
        }

        public JsonEditorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected JsonEditorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}