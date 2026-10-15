using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Input;
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

        /// <summary>
        /// 标题栏：最小化
        /// </summary>
        private void OnMinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 标题栏：关闭
        /// </summary>
        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 标题栏：最大化 / 还原（macOS 绿键）
        /// </summary>
        private void OnZoomClick(object sender, RoutedEventArgs e)
        {
            ToggleZoom();
        }

        /// <summary>
        /// 标题栏：双击缩放，与 macOS 行为一致
        /// </summary>
        private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleZoom();
            }
        }

        private void ToggleZoom()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
    }
}