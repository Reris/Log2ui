using System;
using Log2ui.Data;

namespace Log2ui.Receivers;

public interface IReceiver : IDisposable
{
    string SampleClientConfig { get; }
    string? DisplayName { get; }

    void Initialize();
    void Terminate();

    int Attach(ILogMessageNotifiable notifiable);
    int Detach(ILogMessageNotifiable notifiable);
}
