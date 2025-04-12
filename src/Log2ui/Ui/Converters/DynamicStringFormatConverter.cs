using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;

namespace Log2ui.Ui.Converters;

public class DynamicStringFormatConverter : IMultiValueConverter
{
    public static DynamicStringFormatConverter Instance { get; } = new();

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
