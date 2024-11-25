using Log2ui.Data;

namespace Log2ui.Receivers;

public interface IReceiver
{
    string SampleClientConfig { get; }
    string? DisplayName { get; }

    void Initialize();
    void Terminate();

    void Attach(ILogMessageNotifiable notifiable);
    void Detach(ILogMessageNotifiable notifiable);
}
