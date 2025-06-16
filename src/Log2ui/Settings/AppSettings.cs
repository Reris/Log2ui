using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Log2ui.Settings;

public record AppSettings : INotifyPropertyChanged
{
    private bool _alwaysOnTop;
    private Theme _theme;

    public static AppSettings Default { get; } = new();

    [Category("Appearance")]
    [DisplayName("Theme")]
    [Description("The Log2ui theme.")]
    public Theme Theme
    {
        get => this._theme;
        set => this.SetField(ref this._theme, value);
    }

    [Category("Appearance")]
    [DisplayName("Start Always On Top")]
    [Description("The Log2ui window will remain on top of all other windows.")]
    public bool AlwaysOnTop
    {
        get => this._alwaysOnTop;
        set => this.SetField(ref this._alwaysOnTop, value);
    }

    [Browsable(false)]
    public LoggerSettings LoggerDefaults { get; set; } = LoggerSettings.Default;

    public event PropertyChangedEventHandler? PropertyChanged;

    public AppSettings DeepClone()
    {
        return this with
        {
            LoggerDefaults = this.LoggerDefaults.DeepClone(),
        };
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        this.OnPropertyChanged(propertyName);
        return true;
    }
}
