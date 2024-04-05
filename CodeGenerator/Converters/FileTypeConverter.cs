using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace CodeGenerator.Converters
{
    public class FileTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(value != null, nameof(value) + " != null");
            //判断文件类型
            var split = value.ToString().Split('.');
            var suffix = split.Last();
            const UriKind uriKind = UriKind.RelativeOrAbsolute;
            switch (suffix)
            {
                case "class":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/class.png", uriKind));
                case "css":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/css.png", uriKind));
                case "doc":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/doc.png", uriKind));
                case "dockerfile":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/dockerfile.png", uriKind));
                case "exe":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/exe.png", uriKind));
                case "gitignore":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/gitignore.png", uriKind));
                case "html":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/html.png", uriKind));
                case "iso":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/iso.png", uriKind));
                case "jar":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/jar.png", uriKind));
                case "java":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/java.png", uriKind));
                case "jpg":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/jpg.png", uriKind));
                case "js":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/js.png", uriKind));
                case "json":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/json.png", uriKind));
                case "md":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/md.png", uriKind));
                case "mp3":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/mp3.png", uriKind));
                case "mp4":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/mp4.png", uriKind));
                case "pdf":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/pdf.png", uriKind));
                case "png":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/png.png", uriKind));
                case "ppt":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/ppt.png", uriKind));
                case "py":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/py.png", uriKind));
                case "sql":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/sql.png", uriKind));
                case "svg":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/svg.png", uriKind));
                case "txt":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/txt.png", uriKind));
                case "xls":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/xls.png", uriKind));
                case "xml":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/xml.png", uriKind));
                case "yml":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/yml.png", uriKind));
                case "zip":
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/zip.png", uriKind));
                default:
                    return new BitmapImage(new Uri("/CodeGenerator;component/Images/file.png", uriKind));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}