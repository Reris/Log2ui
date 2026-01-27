using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Log2ui.Receivers;
using Log2ui.Settings;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace Log2ui.Views;

public class ReceiverManagerViewModel : ViewModel, IReceiverManagerViewModel, ISelfRegistering
{
    private readonly ILoggerSettingsViewModel _settingsViewModel;

    public ReceiverManagerViewModel(ILoggerSettingsViewModel settingsViewModel)
    {
        ArgumentNullException.ThrowIfNull(settingsViewModel);

        this._settingsViewModel = settingsViewModel;
        this.ToggleAttachedCommand = ReactiveCommand.CreateFromTask<IReceiverManagerViewModel.ReceiverItem>(this.ToggleAttachedAsync)
                                                    .DisposeWith(this.Disposables);
    }

    public ReactiveCommand<IReceiverManagerViewModel.ReceiverItem, Unit> ToggleAttachedCommand { get; }

    public IObservable<IList<IReceiverManagerViewModel.ReceiverItem>> AllSettings
        => this._settingsViewModel.AllReceiverSettings.CombineLatest(this._settingsViewModel.LoggerSettings)
               .Select(this.ToReceiverItems);

    public ReceiverSettings? CurrentSettings
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<IReceiverManagerViewModel, ReceiverManagerViewModel>();
    }

    public async Task ToggleAttachedAsync(IReceiverManagerViewModel.ReceiverItem item)
    {
        if (item.Attached)
        {
            await this._settingsViewModel.RemoveReceiverAsync(item.Settings.Key);
        }
        else
        {
            await this._settingsViewModel.AddReceiverAsync(item.Settings);
        }
    }

    private IReceiverManagerViewModel.ReceiverItem[] ToReceiverItems((AllReceiverSettings First, NamedLoggerSettings Second) src)
    {
        var (all, logger) = src;
        var result = all.Receivers.Select(a => new IReceiverManagerViewModel.ReceiverItem(logger.ReceiverKeys.Contains(a.Key), a)).ToArray();
        return result;
    }

    public async Task SaveAsync()
    {
        await this._settingsViewModel.SaveAsync();
    }
}
