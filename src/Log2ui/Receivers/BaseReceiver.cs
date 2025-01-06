using System;
using System.Collections.Generic;
using Log2ui.Data;

namespace Log2ui.Receivers;

public abstract class BaseReceiver : IReceiver
{
    private bool _active;
    protected IList<ILogMessageNotifiable> Notifiables { get; } = [];

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public abstract string SampleClientConfig { get; }
    public string? DisplayName { get; protected set; }

    void IReceiver.Initialize()
    {
        if (this._active)
        {
            return;
        }

        this._active = true;
        this.Initialize();
    }

    void IReceiver.Terminate()
    {
        if (!this._active)
        {
            return;
        }

        this._active = false;
        this.Notifiables.Clear();
        this.Terminate();
    }

    public int Attach(ILogMessageNotifiable notifiable)
    {
        if (!this.Notifiables.Contains(notifiable))
        {
            this.Notifiables.Add(notifiable);
            this.OnAttached(notifiable);
        }

        return this.Notifiables.Count;
    }

    public int Detach(ILogMessageNotifiable notifiable)
    {
        if (this.Notifiables.Remove(notifiable))
        {
            this.OnDetached(notifiable);
        }

        return this.Notifiables.Count;
    }

    protected abstract void Initialize();
    protected abstract void Terminate();

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.Terminate();
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
