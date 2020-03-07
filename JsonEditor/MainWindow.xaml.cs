using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
        private Station[] СurStation { get; set; }

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
            ReadJson(dlg.FileName);
            BuildTree();
        }


        private void BuildTree()
        {
            JsonTree.Items.Clear();
            foreach (var station in СurStation)
            {
                BuildNode(JsonTree, station);
            }
        }

        private static void BuildNode<T>(ItemsControl cur, T item)
        {
            var v = new TreeViewItem
            {
                Header = GetProperty(item, "name")
            };
            foreach (var property in (IEnumerable) GetProperty(item, "items") ?? new ArrayList())
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
                    СurStation = JsonConvert.DeserializeObject<Station[]>(data);
                    CurFileName = path;
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Invalid JSON file.");
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Can not read this file.");
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
            if (СurStation != null) return true;
            MessageBox.Show("No opened files", "Can not write JSON.");
            return false;
        }

        private void WriteJson(string path)
        {
            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(СurStation));
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
        internal class Station
        {
            public string type { get; set; }
            public string id { get; set; }
            public string name { get; set; }
            public Arm[] items { get; set; }
        }


        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        internal class Arm
        {
            public string type { get; set; }
            public string id { get; set; }
            public string name { get; set; }
            public Device[] items { get; set; }
        }


        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        internal class Device
        {
            public string type { get; set; }
            public string id { get; set; }
            public string name { get; set; }
        }
    }
}