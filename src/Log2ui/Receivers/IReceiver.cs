using System;
using Log2ui.Data;

namespace Log2ui.Receivers;

public interface IReceiver : IDisposable
{
    string SampleClientConfig { get; }
    string? DisplayName { get; }

    void Initialize();
    void Terminate();

    (bool Attached, int Count) Attach(ILogMessageNotifiable notifiable);
    (bool Detached, int Count) Detach(ILogMessageNotifiable notifiable);
}
