using System;
using Material.Icons;

namespace Log2ui.Views;

public class LogSearchMode(string name, MaterialIconKind icon, string tip, Func<string, Func<string, bool>> build)
{
    public string Name { get; } = name;
    public MaterialIconKind Icon { get; } = icon;
    public bool Underscore { get; init; }
    public string Tip { get; } = tip;
    public Func<string, Func<string, bool>> Build { get; } = build;
}
