using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Log2ui.Ui;

public class EqualsConverter : IValueConverter
{
    public static EqualsConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => object.Equals(value, parameter);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => AvaloniaProperty.UnsetValue;
}
