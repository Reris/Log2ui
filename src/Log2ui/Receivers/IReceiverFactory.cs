using Log2ui.Data;
using Log2ui.Settings;

namespace Log2ui.Receivers;

public interface IReceiverFactory
{
    void Detach(ReceiverSettings settings, ILogMessageNotifiable notify);
    void Attach(ReceiverSettings settings, ILogMessageNotifiable notify);
}
