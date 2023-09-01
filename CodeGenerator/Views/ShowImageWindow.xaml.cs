using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CodeGenerator.Views
{
    public partial class ShowImageWindow : Window
    {
        public ShowImageWindow(FileInfo file)
        {
            InitializeComponent();
            
            Title = file.Name;
            using (var reader = new BinaryReader(File.Open(file.FullName, FileMode.Open)))
            {
                var fi = new FileInfo(file.FullName);
                var bytes = reader.ReadBytes((int)fi.Length);
                reader.Close();

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = new MemoryStream(bytes);
                bitmapImage.EndInit();
                BigImageViewer.ImageSource = BitmapFrame.Create(bitmapImage);
            }
        }
    }
}