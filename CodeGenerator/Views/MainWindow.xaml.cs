using CodeGenerator.DataService.Impl;
using CodeGenerator.ViewModels;

namespace CodeGenerator.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel(new AppDataServiceImpl());
        }
    }
}