using System;
using System.ComponentModel;
using Log2ui.Data;

namespace Log2ui.Receivers;

[Serializable]
public abstract class BaseReceiver : MarshalByRefObject, IReceiver
{
    [NonSerialized]
    private string? _displayName;

    [NonSerialized]
    protected ILogMessageNotifiable? Notifiable;

    public abstract string SampleClientConfig { get; }

    [Browsable(false)]
    public string? DisplayName
    {
        get => this._displayName;
        protected set => this._displayName = value;
    }

    public abstract void Initialize();
    public abstract void Terminate();

    public virtual void Attach(ILogMessageNotifiable notifiable) => this.Notifiable = notifiable;
    public virtual void Detach(ILogMessageNotifiable notifiable)
    {
        this.Notifiable = null;
        this.Terminate();
    }
}
