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
        private Station[] СurStations { get; set; }

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
            var oldStations = СurStations;
            var oldFileName = CurFileName;
            try
            {
                ReadJson(dlg.FileName);
                BuildTree();
            }
            catch (JsonEditorException exception)
            {
                MessageBox.Show(exception.Message, "Can not read the file!");
                CurFileName = oldFileName;
                СurStations = oldStations;
            }
        }


        private void BuildTree()
        {
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
                throw new JsonEditorException("Invalid JSON file.", e);
            }
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
            try
            {
                var data = File.ReadAllText(path);
                try
                {
                    СurStations = JsonConvert.DeserializeObject<Station[]>(data);
                    CurFileName = path;
                }
                catch (Exception exception)
                {
                    throw new JsonEditorException("The file is not JSON.", exception);
                }
            }
            catch (Exception exception)
            {
                throw new JsonEditorException("Can not read this file.", exception);
            }
        }

        private void SaveAsButtonClick(object sender, RoutedEventArgs e)
        {
            if (!CheckDataToWrite()) return;
            var dlg = new SaveFileDialog {Filter = JsonFilesFilter};
            if (dlg.ShowDialog() != true) return; //can be null
            WriteJson(dlg.FileName);
            CurFileName = dlg.FileName;
        }

        private bool CheckDataToWrite()
        {
            if (СurStations != null) return true;
            MessageBox.Show("No opened files", "Can not write JSON.");
            return false;
        }

        private void WriteJson(string path)
        {
            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(СurStations));
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Can not write to this file.");
            }
        }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            if (CheckDataToWrite())
            {
                WriteJson(CurFileName);
            }
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