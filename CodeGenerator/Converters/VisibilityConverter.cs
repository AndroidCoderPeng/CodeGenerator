using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace CodeGenerator.Converters
{
    public class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                Debug.Assert(parameter != null, nameof(parameter) + " != null");
                if (parameter.Equals("ProgressBar"))
                {
                    if (value == null)
                    {
                        return "Visible";
                    }

                    return int.Parse(value.ToString()) == 100 ? "Collapsed" : "Visible";
                }
                else
                {
                    if (value == null)
                    {
                        return "Collapsed";
                    }

                    return int.Parse(value.ToString()) == 100 ? "Visible" : "Collapsed";
                }
            }
            catch
            {
                return "Collapsed";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}