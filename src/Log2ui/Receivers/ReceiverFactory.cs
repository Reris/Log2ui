using System;
using System.Collections.ObjectModel;
using System.Linq;
using Log2ui.Data;
using Log2ui.Dependencies;
using Log2ui.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Receivers;

public class ReceiverFactory(IServiceProvider serviceProvider) : IReceiverFactory, ISelfRegistering
{
    public ObservableCollection<IReceiverFactory.AttachedReceivers> Attached { get; } = [];

    public void Detach(ReceiverSettings settings, ILogMessageNotifiable notify)
    {
        var attached = this.Attached.FirstOrDefault(a => a.Settings == settings);
        if (attached.Receiver is null)
        {
            return;
        }

        var current = attached.Receiver.Attach(notify);
        if (current == 0)
        {
            attached.Receiver.Terminate();
        }

        this.Attached.Remove(attached);
    }

    public void Attach(ReceiverSettings settings, ILogMessageNotifiable notify)
    {
        var receiver = this.Attached.FirstOrDefault(a => a.Settings == settings).Receiver;
        if (receiver is null)
        {
            receiver = settings.CreateReceiver(serviceProvider);
            receiver.Initialize();
        }

        receiver.Attach(notify);
        this.Attached.Add(new IReceiverFactory.AttachedReceivers(settings, receiver));
    }

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddSingleton<IReceiverFactory, ReceiverFactory>();
    }
}
