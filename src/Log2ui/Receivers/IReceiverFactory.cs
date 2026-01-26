using System.Collections.ObjectModel;
using Log2ui.Data;
using Log2ui.Settings;

namespace Log2ui.Receivers;

public interface IReceiverFactory
{
    ObservableCollection<AttachedReceivers> Attached { get; }

    void Detach(ReceiverSettings settings, ILogMessageNotifiable notify);
    void Attach(ReceiverSettings settings, ILogMessageNotifiable notify);


    record struct AttachedReceivers(ReceiverSettings Settings, IReceiver Receiver);
}
