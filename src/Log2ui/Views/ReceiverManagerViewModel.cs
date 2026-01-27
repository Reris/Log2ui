using System;
using Log2ui.Dependencies;
using Log2ui.Receivers;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Views;

public class ReceiverManagerViewModel : ViewModel, IReceiverManagerViewModel, ISelfRegistering
{
    private readonly IReceiverFactory _receiverFactory;
    private readonly ILoggerSettingsViewModel _settingsViewModel;

    public ReceiverManagerViewModel(IReceiverFactory receiverFactory, ILoggerSettingsViewModel settingsViewModel)
    {
        ArgumentNullException.ThrowIfNull(receiverFactory);
        ArgumentNullException.ThrowIfNull(settingsViewModel);

        this._receiverFactory = receiverFactory;
        this._settingsViewModel = settingsViewModel;
    }

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<IReceiverManagerViewModel, ReceiverManagerViewModel>();
    }
}
