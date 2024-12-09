using System;
using System.Collections.Generic;
using System.Linq;
using Log2ui.Data;

namespace Log2ui.Receivers;

public abstract class BaseReceiver : IReceiver
{
    protected IList<ILogMessageNotifiable> Notifiables { get; } = [];

    public abstract string SampleClientConfig { get; }
    public string? DisplayName { get; protected set; }

    public abstract void Initialize();
    public abstract void Terminate();

    public void Attach(ILogMessageNotifiable notifiable)
    {
        this.Notifiables.Add(notifiable);
        if (this.Notifiables.Count == 1)
        {
            this.Initialize();
        }

        this.OnAttached(notifiable);
    }

    public void Detach(ILogMessageNotifiable notifiable)
    {
        this.Notifiables.Remove(notifiable);
        this.OnDetached(notifiable);
        if (this.Notifiables.Count == 0)
        {
            this.Terminate();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var notifiable in this.Notifiables.ToArray())
        {
            this.Detach(notifiable);
        }
    }

    public virtual void Notify(LogMessage logMsg)
    {
        foreach (var notifiable in this.Notifiables)
        {
            notifiable.Notify(logMsg);
        }
    }

    public virtual void Notify(IReadOnlyList<LogMessage> logMsg)
    {
        foreach (var notifiable in this.Notifiables)
        {
            notifiable.Notify(logMsg);
        }
    }

    protected virtual void OnAttached(ILogMessageNotifiable notifiable)
    {
    }

    protected virtual void OnDetached(ILogMessageNotifiable notifiable)
    {
    }
}
