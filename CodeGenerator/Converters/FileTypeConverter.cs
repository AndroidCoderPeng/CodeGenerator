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

            var fileExtension = value.ToString().Trim().ToLower();

            if (fileExtension.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/class.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/css.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".doc", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/doc.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".dockerfile", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/dockerfile.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/exe.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".gitignore", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/gitignore.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/html.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/iso.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/jar.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".java", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/java.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/jpg.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/js.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/json.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/md.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/mp3.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/mp4.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/pdf.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/png.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".ppt", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/ppt.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/py.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/sql.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/svg.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/txt.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/xls.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/xml.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/yml.png", UriTypeKind));
            }

            if (fileExtension.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri("/CodeGenerator;component/Images/zip.png", UriTypeKind));
            }

            return new BitmapImage(new Uri("/CodeGenerator;component/Images/file.png", UriTypeKind));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}