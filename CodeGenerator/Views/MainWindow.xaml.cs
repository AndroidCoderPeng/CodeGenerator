using System.Windows;
using System.Windows.Controls;
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
            _eventAggregator.GetEvent<DirectoryEvent>().Publish(DirListBox.SelectedIndex);
        }
        
        private void DeleteFileButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button))
            {
                return;
            }

            _eventAggregator.GetEvent<FileNameTagEvent>().Publish(button.Tag.ToString());
        }

        private void DeleteFileSuffixButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button))
            {
                return;
            }

            _eventAggregator.GetEvent<FileSuffixTagEvent>().Publish(button.Tag.ToString());
        }
    }
}