using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;

namespace JsonEditor
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Station[] curStation = new Station[0];
        private string curFileName = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            var dlg = new Microsoft.Win32.OpenFileDialog();
            if (dlg.ShowDialog() != true) return;
            try
            {
                var data = System.IO.File.ReadAllText(dlg.FileName);
                try
                {
                    curStation = JsonConvert.DeserializeObject<Station[]>(data);
                    curFileName = dlg.FileName;
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

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