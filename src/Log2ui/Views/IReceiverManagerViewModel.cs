using Log2ui.Settings;

namespace Log2ui.Views;

public interface IReceiverManagerViewModel
{
    record ReceiverItem(bool Attached, ReceiverSettings Settings);
}
