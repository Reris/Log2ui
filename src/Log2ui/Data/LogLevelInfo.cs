using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using ReactiveUI;
using Color = System.Drawing.Color;

namespace Log2ui.Data;

[Serializable]
public class LogLevelInfo : ReactiveObject
{
    private readonly ObservableAsPropertyHelper<IBrush> _brush;
    private Color? _customColor;

    public LogLevelInfo(LogLevel level, Color color)
        : this(level, level.ToString(), color, color)
    {
    }

    public LogLevelInfo(LogLevel level, string name, Color lightColor, Color darkColor)
    {
        this.Level = level;
        this.Name = name;
        this.LightColor = lightColor;
        this.DarkColor = darkColor;

        this._brush = this.WhenAnyValue(a => a.CustomColor)
                          .CombineLatest(Application.Current.WhenAnyValue(a => a.ActualThemeVariant))
                          .Select(
                              a => a switch
                              {
                                  { First: not null } => a.First.Value,
                                  { Second.Key: "Dark" } => this.DarkColor,
                                  _ => this.LightColor
                              })
                          .Select(a => new ImmutableSolidColorBrush(new Avalonia.Media.Color(a.A, a.R, a.G, a.B)))
                          .ToProperty(this, a => a.Brush);
    }

    public Color LightColor { get; }
    public Color DarkColor { get; }

    public Color? CustomColor
    {
        get => this._customColor;
        set => this.RaiseAndSetIfChanged(ref this._customColor, value);
    }

    public LogLevel Level { get; }
    public string Name { get; }
    public IBrush Brush => this._brush.Value;

    public override bool Equals(object? obj)
    {
        if (obj is LogLevelInfo info)
        {
            return info.Level == this.Level;
        }

        return false;
    }

    public override int GetHashCode() => this.Level.GetHashCode();

    public static bool operator ==(LogLevelInfo? x, LogLevelInfo? y) => EqualityComparer<LogLevelInfo>.Default.Equals(x, y);
    public static bool operator !=(LogLevelInfo? x, LogLevelInfo? y) => !EqualityComparer<LogLevelInfo>.Default.Equals(x, y);
    public static bool operator >(LogLevelInfo? x, LogLevelInfo? y) => (x?.Level ?? LogLevel.Invalid) > (y?.Level ?? LogLevel.Invalid);
    public static bool operator <(LogLevelInfo? x, LogLevelInfo? y) => (x?.Level ?? LogLevel.Invalid) < (y?.Level ?? LogLevel.Invalid);
    public static bool operator >=(LogLevelInfo? x, LogLevelInfo? y) => (x?.Level ?? LogLevel.Invalid) >= (y?.Level ?? LogLevel.Invalid);
    public static bool operator <=(LogLevelInfo? x, LogLevelInfo? y) => (x?.Level ?? LogLevel.Invalid) <= (y?.Level ?? LogLevel.Invalid);
}
