using Log2ui.Settings;

namespace Log2ui.Views;

public interface IReceiverManagerViewModel
{
    record AddReceiverSettings(ReceiverSettings Settings);

    record ReceiverItem(bool Attached, bool IsNew, ReceiverSettings Settings);
}
