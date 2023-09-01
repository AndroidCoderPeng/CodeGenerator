using System.IO;
using System.Windows;

namespace CodeGenerator.Views
{
    public partial class ShowTextWindow : Window
    {
        public ShowTextWindow(FileInfo file)
        {
            InitializeComponent();

            Title = file.Name;
            ContentTextBox.Text = File.ReadAllText(file.FullName);
        }
    }
}