using System.Windows;

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
        }
        
        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            // 获取主屏幕的分辨率
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            // 判断是否为 768x1366 分辨率
            if ((int)screenWidth == 1366 && (int)screenHeight == 768)
            {
                // 如果是 768x1366 分辨率，则全屏显示
                WindowState = WindowState.Maximized;
            }
            else
            {
                // 如果不是 768x1366 分辨率，则设置窗口大小为 768x1366
                WindowState = WindowState.Normal;
                Width = 1366;
                Height = 768;
                // 允许通过鼠标拖动窗口
                MouseDown += delegate { DragMove(); };
            }
        }
    }
}