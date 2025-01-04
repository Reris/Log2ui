using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Log2ui.Ui.Converters;

public class IfConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not IfCase ifCase)
        {
            return AvaloniaProperty.UnsetValue;
        }

        var result = object.Equals(ifCase.When, value);
        return result ? ifCase.Then : ifCase.Else;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
