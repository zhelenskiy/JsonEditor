using System.Windows;

namespace JsonEditor
{
    /// <summary>
    /// Окно педназначенно для переименования
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