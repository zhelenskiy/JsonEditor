using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace JsonEditor
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Station[] _curStation;
        private string _curFileName = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenButtonClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog() != true) return;
            ReadJson(dlg.FileName);
        }

        private void ReadJson(string path)
        {
            try
            {
                var data = File.ReadAllText(path);
                try
                {
                    _curStation = JsonConvert.DeserializeObject<Station[]>(data);
                    _curFileName = path;
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
            var dlg = new SaveFileDialog();
            if (dlg.ShowDialog() != true) return;
            WriteJson(dlg.FileName);
        }

        private bool CheckDataToWrite()
        {
            if (_curStation != null) return true;
            MessageBox.Show("No opened files", "Can not write JSON.");
            return false;
        }

        private void WriteJson(string path)
        {
            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(_curStation));
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
                WriteJson(_curFileName);
            }
        }
    }

    internal class Station
    {
        public string type;
        public string id;
        public string name;
        public Arm[] items;
    }

    internal class Arm
    {
        public string type;
        public string id;
        public string name;
        public Device[] items;
    }

    internal class Device
    {
        public string type;
        public string id;
        public string name;
    }
}