using Avalonia.Data.Converters;
using Avalonia;
using System.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Log2ui.Ui.Converters;

public class DynamicStringFormatConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] is string format)
        {
            return string.Format(culture, format, values.Skip(1).ToArray());
        }

        return AvaloniaProperty.UnsetValue;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
