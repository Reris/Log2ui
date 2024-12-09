using Log2ui.Receivers;

namespace Log2ui.Views;

public interface ILoggerViewModel : IViewModel
{
    string Name { get; }
    void AttachTo(IReceiver receiver);
}
