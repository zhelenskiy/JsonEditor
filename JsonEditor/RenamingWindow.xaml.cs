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
using System.Windows.Shapes;

namespace JsonEditor
{
    /// <summary>
    /// Interaction logic for RenamingWindow.xaml
    /// </summary>
    public partial class RenamingWindow : Window
    {
        public RenamingWindow()
        {
            InitializeComponent();
        }
        public RenamingWindow(string initialString) : this()
        {
            ResultName = initialString;
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        public string ResultName
        {
            get => NewName.Text;
            private set => NewName.Text = value;
        }
    }
}
