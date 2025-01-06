using System;
using System.Collections.Generic;
using System.Linq;
using Log2ui.Data;
using Log2ui.Dependencies;
using Log2ui.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Receivers;

public class ReceiverFactory(IServiceProvider serviceProvider) : IReceiverFactory, ISelfRegistering
{
    private readonly IList<AttachedReceivers> _attached = [];

    public void Detach(ReceiverSettings settings, ILogMessageNotifiable notify)
    {
        var attached = this._attached.FirstOrDefault(a => a.Settings == settings);
        if (attached.Receiver is null)
        {
            return;
        }

        var current = attached.Receiver.Attach(notify);
        if (current == 0)
        {
            attached.Receiver.Terminate();
        }

        this._attached.Remove(attached);
    }

    public void Attach(ReceiverSettings settings, ILogMessageNotifiable notify)
    {
        var receiver = this._attached.FirstOrDefault(a => a.Settings == settings).Receiver;
        if (receiver is null)
        {
            receiver = settings.CreateReceiver(serviceProvider);
            receiver.Initialize();
        }

        receiver.Attach(notify);
        this._attached.Add(new AttachedReceivers(settings, receiver));
    }

    public static void RegisterServices(Registry registry)
    {
        registry.Collection.AddSingleton<IReceiverFactory, ReceiverFactory>();
    }

    private record struct AttachedReceivers(ReceiverSettings Settings, IReceiver Receiver);
}
