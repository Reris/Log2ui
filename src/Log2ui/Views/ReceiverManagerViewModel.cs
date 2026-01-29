using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Log2ui.Settings;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using static Log2ui.Views.IReceiverManagerViewModel;

namespace Log2ui.Views;

public class ReceiverManagerViewModel : ViewModel, IReceiverManagerViewModel, ISelfRegistering
{
    private readonly Subject<ReceiverSettings?> _currentSettingsChanged;
    private readonly ILoggerSettingsViewModel _settingsViewModel;

    public ReceiverManagerViewModel(ILoggerSettingsViewModel settingsViewModel, ReceiverSettings[] availableSettings)
    {
        ArgumentNullException.ThrowIfNull(settingsViewModel);

        this._settingsViewModel = settingsViewModel;
        this.WhenAnyValue(a => a.CurrentSettings, a => a.NewSettings)
            .Subscribe(_ => this.IsNew = this.CurrentSettings == this.NewSettings)
            .DisposeWith(this.Disposables);

        this.AddReceiverSettings = availableSettings.Select(a => new AddReceiverSettings(a)).OrderBy(a => a.Settings.TypeDisplayName).ToArray();
        this.ToggleAttachedCommand = ReactiveCommand.CreateFromTask<ReceiverItem>(this.ToggleAttachedAsync)
                                                    .DisposeWith(this.Disposables);

        this._currentSettingsChanged = new Subject<ReceiverSettings?>();

        this.AllSettings = this._settingsViewModel.AllReceiverSettings.CombineLatest(this._settingsViewModel.LoggerSettings, this.WhenAnyValue(a => a.NewSettings))
                               .Select(this.ToReceiverItems)
                               .ToObservableCollection(this.Disposables);

        this.SaveNewSettingsCommand = ReactiveCommand.CreateFromTask(this.SaveNewSettingsAsync, this.CurrentSettingsHasDuplicateKey.Select(a => !a))
                                                     .DisposeWith(this.Disposables);
    }

    public IList<AddReceiverSettings> AddReceiverSettings { get; }
    public ReactiveCommand<ReceiverItem, Unit> ToggleAttachedCommand { get; }
    public ReadOnlyObservableCollection<ReceiverItem> AllSettings { get; }
    public ReactiveCommand<Unit, Unit> SaveNewSettingsCommand { get; }

    public IObservable<bool> CurrentSettingsHasDuplicateKey => this._currentSettingsChanged
                                                                   .Select(a => this.AllSettings.Any(b => b.Settings != a && b.Settings.Key == a?.Key));

    public bool IsNew
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ReceiverSettings? CurrentSettings
    {
        get;
        set
        {
            field?.PropertyChanged -= this.PushCurrentSettingsChanged;
            this.RaiseAndSetIfChanged(ref field, value);
            field?.PropertyChanged += this.PushCurrentSettingsChanged;
            this.PushCurrentSettingsChanged();
        }
    }

    public ReceiverSettings? NewSettings
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<IReceiverManagerViewModel, ReceiverManagerViewModel>();
    }

    private void PushCurrentSettingsChanged(object? sender = null, PropertyChangedEventArgs? args = null)
    {
        this._currentSettingsChanged.OnNext(this.CurrentSettings);
    }

    public async Task SaveNewSettingsAsync()
    {
        ArgumentNullException.ThrowIfNull(this.NewSettings);

        var settings = this.NewSettings;
        await this._settingsViewModel.AddReceiverAsync(settings);
        this.NewSettings = null;
    }

    public void CreateReceiver(ReceiverSettings receiverSettings)
    {
        this.CurrentSettings = this.NewSettings = receiverSettings.DeepClone();
    }

    public async Task ToggleAttachedAsync(ReceiverItem item)
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

    private ReceiverItem[] ToReceiverItems((AllReceiverSettings First, NamedLoggerSettings Second, ReceiverSettings? Third) src)
    {
        var (all, logger, create) = src;

        var result = all.Receivers.Select(a => new ReceiverItem(logger.ReceiverKeys.Contains(a.Key), false, a));
        if (create is not null)
        {
            result = result.Append(new ReceiverItem(false, true, create));
        }

        return result.ToArray();
    }

    public async Task SaveAsync()
    {
        await this._settingsViewModel.SaveAsync();
    }

    public async Task DeleteCurrentReceiverAsync()
    {
        if (this.CurrentSettings is null || this.CurrentSettings == this.NewSettings)
        {
            this.CurrentSettings = this.NewSettings = null;
            return;
        }

        await this._settingsViewModel.DeleteReceiverAsync(this.CurrentSettings.Key);
    }

    public int CountAttachedLoggers(string receiverKey)
    {
        if (this.NewSettings?.Key == receiverKey)
        {
            return 0;
        }

        return this._settingsViewModel.CountAttachedLoggers(receiverKey);
    }
}
