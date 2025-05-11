using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace CodeGenerator.Converters
{
    public class FileTypeConverter : IValueConverter
    {
        private const UriKind UriTypeKind = UriKind.RelativeOrAbsolute;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/file.png", UriTypeKind));
            }

            switch (value)
            {
                case ".class":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/class.png", UriTypeKind));
                case ".css":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/css.png", UriTypeKind));
                case ".doc":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/doc.png", UriTypeKind));
                case ".dockerfile":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/dockerfile.png", UriTypeKind));
                case ".exe":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/exe.png", UriTypeKind));
                case ".gitignore":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/gitignore.png", UriTypeKind));
                case ".html":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/html.png", UriTypeKind));
                case ".iso":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/iso.png", UriTypeKind));
                case ".jar":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/jar.png", UriTypeKind));
                case ".java":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/java.png", UriTypeKind));
                case ".jpg":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/jpg.png", UriTypeKind));
                case ".js":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/js.png", UriTypeKind));
                case ".json":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/json.png", UriTypeKind));
                case ".md":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/md.png", UriTypeKind));
                case ".mp3":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/mp3.png", UriTypeKind));
                case ".mp4":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/mp4.png", UriTypeKind));
                case ".pdf":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/pdf.png", UriTypeKind));
                case ".png":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/png.png", UriTypeKind));
                case ".ppt":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/ppt.png", UriTypeKind));
                case ".py":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/py.png", UriTypeKind));
                case ".sql":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/sql.png", UriTypeKind));
                case ".svg":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/svg.png", UriTypeKind));
                case ".txt":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/txt.png", UriTypeKind));
                case ".xls":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/xls.png", UriTypeKind));
                case ".xml":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/xml.png", UriTypeKind));
                case ".yml":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/yml.png", UriTypeKind));
                case ".zip":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/zip.png", UriTypeKind));
                default:
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/file.png", UriTypeKind));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}