using System;

namespace Log2ui.Views;

public class LogSearchMode
{
    public LogSearchMode(string name, string icon, string tip, Func<string, Func<string, bool>> build)
    {
        this.Name = name;
        this.Icon = icon;
        this.Tip = tip;
        this.Build = build;
    }

    public string Name { get; }
    public string Icon { get; }
    public string Tip { get; }
    public Func<string, Func<string, bool>> Build { get; }
}
