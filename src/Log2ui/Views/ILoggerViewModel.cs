using System.Threading.Tasks;
using Log2ui.Settings;

namespace Log2ui.Views;

public interface ILoggerViewModel : ICaptionedViewModel
{
    string Name { get; }
    Task<bool> AttachToAsync(ReceiverSettings receiverSettings);
}
