using System;
using Avalonia.Markup.Xaml;

namespace Log2ui.Ui.Converters;

public class IfCase : MarkupExtension
{
    public object? When { get; set; } = true;
    public object? Then { get; set; }
    public object? Else { get; set; }

    public override string ToString()
    {
        return $"When={this.When}; Then={this.Then}";
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}
