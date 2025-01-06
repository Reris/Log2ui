using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Log2ui.Receivers;

namespace Log2ui.Settings;

public abstract record ReceiverSettings
{
    /// <summary>
    /// Defines a Key-Value for a receiver to share receivers among ressources and it opens only once. Like the File-Fullname in a CSV
    /// </summary>
    [Browsable(false)]
    [JsonIgnore]
    public abstract string ValueKey { get; }

    public abstract ReceiverSettings DeepClone();
    public abstract IReceiver CreateReceiver(IServiceProvider serviceProvider);
}
