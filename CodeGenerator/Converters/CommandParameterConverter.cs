using System;
using System.Globalization;
using System.Windows.Data;
using CodeGenerator.Utils;

namespace CodeGenerator.Converters
{
    public class CommandParameterConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var type = (string)values[0];
            var boundValue = (string)values[1];
            return new CommandParameter(type, boundValue);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}