using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Log2ui.Exporters;

public abstract record ExportSettings : INotifyPropertyChanged
{
    [Category("General")]
    [DisplayName("Display name")]
    [ReadOnly(true)]
    public abstract string DisplayName { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    public abstract ExportSettings DeepClone();

    public abstract IExport CreateExporter(IServiceProvider serviceProvider);

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
