using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Log2ui.Collections;
using Log2ui.Receivers;

namespace Log2ui.Settings;

public abstract record ReceiverSettings : INotifyPropertyChanged
{
    protected ReceiverSettings(EquatableArray<FieldMapping> defaultMappings)
    {
        this.Mappings = defaultMappings;
    }

    /// <summary>
    /// Defines a Key for a receiver to share receivers among ressources and it opens only once. Like the File-Fullname in a CSV
    /// </summary>
    [Browsable(false)]
    [JsonPropertyName("__key__")]
    public abstract string Key { get; }

    [JsonIgnore]
    [Category("General")]
    [DisplayName("Display name")]
    [ReadOnly(true)]
    public abstract string DisplayName { get; }

    [JsonIgnore]
    [Category("General")]
    [DisplayName("Receiver type")]
    public abstract string TypeDisplayName { get; }

    [Browsable(false)]
    public EquatableArray<FieldMapping> Mappings
    {
        get;
        set => this.SetField(ref field, value);
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
        this.OnPropertyChanged(nameof(this.DisplayName));
        return true;
    }

    protected static string CreateKey(string discriminator, params ReadOnlySpan<object?> values)
    {
        return string.Join('_', values.ToArray().Where(await => await is not null).Prepend(discriminator));
    }
}
