using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Metadata;

namespace Log2ui.Ui.Converters;

/// <example>
///     <![CDATA[
///  <con:SwitchConverter x:Key="MyStyleConverter" Default="{x:Static FontWeights.Normal}">
///     <con:SwitchCase When="pageHeader" Then="{x:Static FontWeights.Bold}" />
///     <con:SwitchCase When="header" Then="{x:Static FontWeights.SemiBold}" />
///     <con:SwitchCase When="smallText" Then="{x:Static FontWeights.Light}" />
///     <con:SwitchCase When="tinyText" Then="{x:Static FontWeights.Thin}" />
/// </con:SwitchConverter>
/// <TextBlock FontWeight="{Binding Style, Converter={StaticResource MyStyleConverter}}" />
///  ]]>
/// </example>
public class SwitchConverter : IValueConverter
{
    [Content]
    public List<SwitchCase> Cases { get; } = [];

    public object? Default { get; set; } = AvaloniaProperty.UnsetValue;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return AvaloniaProperty.UnsetValue;
        }

        var result = this.Cases.FirstOrDefault(c => object.Equals(c.When, value));
        return result != null ? result.Then : this.Default;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
