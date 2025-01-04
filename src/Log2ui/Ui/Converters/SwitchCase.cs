using Avalonia.Metadata;

namespace Log2ui.Ui.Converters;

public class SwitchCase(object? when, object? then)
{
    public object? When { get; set; } = when;

    [Content]
    public object? Then { get; set; } = then;

    public override string ToString()
    {
        return $"When={this.When}; Then={this.Then}";
    }
}
