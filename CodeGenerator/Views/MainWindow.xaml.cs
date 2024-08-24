using System.Windows;
using CodeGenerator.Events;
using Prism.Events;

namespace CodeGenerator.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly IEventAggregator _eventAggregator;

        public MainWindow(IEventAggregator eventAggregator)
        {
            InitializeComponent();

            _eventAggregator = eventAggregator;
        }

        private void MenuItem_DeleteButtonOnClick(object sender, RoutedEventArgs e)
        {
            _eventAggregator.GetEvent<DirectoryEvent>().Publish(FolderListBox.SelectedIndex);
        }
    }
}