using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Log2ui.Collections;
using Log2ui.Receivers;

namespace Log2ui.Settings;

public abstract record ReceiverSettings : INotifyPropertyChanged
{
    private EquatableArray<LogColumn> _properties;

    protected ReceiverSettings(EquatableArray<LogColumn> defaultProperties)
    {
        this._properties = defaultProperties;
    }

    /// <summary>
    /// Defines a Key-Value for a receiver to share receivers among ressources and it opens only once. Like the File-Fullname in a CSV
    /// </summary>
    [Browsable(false)]
    [JsonIgnore]
    public abstract string ValueKey { get; }

    [Browsable(false)]
    public EquatableArray<LogColumn> Properties
    {
        get => this._properties;
        set => this.SetField(ref this._properties, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public abstract ReceiverSettings DeepClone();
    public abstract IReceiver CreateReceiver(IServiceProvider serviceProvider);

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
