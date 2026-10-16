using System.Runtime.Versioning;
using System.Windows;
using CodeGenerator.Utils;
using CodeGenerator.ViewModels;

namespace CodeGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [SupportedOSPlatform("windows6.1")]
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel(new DialogService());
        }
    }
}