using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Threading.Tasks;
using DynamicData;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Log2ui.Receivers;
using Log2ui.Settings.Services;
using Log2ui.Tools;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Serilog;

namespace Log2ui.Views;

public class MainWindowViewModel : ViewModel, ISelfRegistering, ILoading
{
    private static readonly ILogger Logger = Log.ForContext<MainWindowViewModel>();

    private readonly IMainDispatcher _mainDispatcher;
    private readonly IViewModelFactory _viewModelFactory;
    private bool _alwaysOnTop;
    private ICaptionedViewModel? _selected;

    public MainWindowViewModel(IMainDispatcher mainDispatcher, IViewModelFactory viewModelFactory, ISettingsService settingsService)
    {
        UnhandledExceptionHandler.Register(this);

        this._mainDispatcher = mainDispatcher;
        this._viewModelFactory = viewModelFactory;
        this.CreateLoggerFunc = () => this.CreateLogger();
        this.AppSettingsCommand = ReactiveCommand.Create(this.OpenAppSettings).DisposeWith(this.Disposables);
        this.ContentItems.CollectionChanged += this.ContentItemsOnCollectionChanged;
        this.Loading = this.LoadAsync(settingsService);
    }

    public ObservableCollection<ICaptionedViewModel> ContentItems { get; } = [];

    public ICaptionedViewModel? Selected
    {
        get => this._selected;
        set => this.RaiseAndSetIfChanged(ref this._selected, value);
    }

    public ReactiveCommand<Unit, Unit> AppSettingsCommand { get; }
    public Func<ILoggerViewModel> CreateLoggerFunc { get; }

    public bool AlwaysOnTop
    {
        get => this._alwaysOnTop;
        private set => this.RaiseAndSetIfChanged(ref this._alwaysOnTop, value);
    }

    public Task Loading { get; }

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<MainWindowViewModel>();
    }

    public void ToggleAlwaysOnTop()
    {
        this.AlwaysOnTop = !this.AlwaysOnTop;
    }

    public async Task LoadAsync(ISettingsService settingsService)
    {
        await settingsService.Loading;
        await LoadSettingsAsync();
        await Task.WhenAll(AddAtLeastOne(), this.CreateInternalLoggerAsync());
        return;

        async Task LoadSettingsAsync()
        {
            var settings = await settingsService.AppSettings.GetCurrentAsync();
            this.AlwaysOnTop = settings.AlwaysOnTop;

            var unshelfLoggers = settingsService.AllLoggerNames
                                                .Where(a => this.ContentItems.OfType<ILoggerViewModel>().All(b => b.Name != a))
                                                .Select(this.CreateLogger);
            this.ContentItems.AddRange(unshelfLoggers);
        }

        async Task AddAtLeastOne()
        {
            if (this.ContentItems.Count == 0)
            {
                await this.AddLoggerAsync();
            }
        }
    }

    private ILoggerViewModel CreateLogger(string? withName = null)
    {
        withName ??= this.CreateLoggerName();
        return this._viewModelFactory.Create<ILoggerViewModel>(withName);
    }

    private void OpenAppSettings()
    {
        if (this.ContentItems.OfType<AppSettingsViewModel>().FirstOrDefault() is { } alreadyOpen)
        {
            this.Selected = alreadyOpen;
            return;
        }

        var settingsVm = this._viewModelFactory.Create<AppSettingsViewModel>();
        this.ContentItems.Add(settingsVm);
        this.Selected = settingsVm;
    }

    private void ContentItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Remove:
            {
                var viewModel = e.OldItems!.Cast<ICaptionedViewModel>().Single();
                var disposable = viewModel as IDisposable;
                if (viewModel is IClosed closed)
                {
                    this._mainDispatcher.InvokeAsync(
                        async _ =>
                        {
                            await closed.ClosedAsync();
                            disposable?.Dispose();
                        }).FireAndForget();
                }
                else
                {
                    disposable?.Dispose();
                }

                MainWindowViewModel.Logger.Information("Removed Content Item {Caption}", viewModel.Caption);

                KeepAtLeastOne();

                break;
            }
            case NotifyCollectionChangedAction.Add:
            {
                var viewModel = e.NewItems?.Cast<ICaptionedViewModel>().FirstOrDefault();
                MainWindowViewModel.Logger.Information("Added Content Item {Caption}", viewModel?.Caption);
                break;
            }
            case NotifyCollectionChangedAction.Replace:
                break;
            case NotifyCollectionChangedAction.Move:
                break;
            case NotifyCollectionChangedAction.Reset:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        void KeepAtLeastOne()
        {
            if (this.ContentItems.Count == 0)
            {
                this._mainDispatcher.InvokeAsync(
                    async _ =>
                    {
                        await Task.Yield();
                        await this.AddLoggerAsync();
                    });
            }
        }
    }

    private string CreateLoggerName()
    {
        for (var i = 1; i < 10000; i++)
        {
            var name = $"Log{i}";
            if (this.ContentItems.OfType<ILoggerViewModel>().All(a => a.Name != name))
            {
                return name;
            }
        }

        throw new IndexOutOfRangeException("Too many loggers.");
    }

    private async Task AddLoggerAsync()
    {
        var logger = this.CreateLogger();
        this.ContentItems.Add(logger);
        this.Selected = logger;
        await logger.AttachToAsync(new TcpReceiver.Settings());
    }


    private async Task CreateInternalLoggerAsync()
    {
        const string internalLogger = "Log2ui-Log";

        var logger = this.ContentItems.OfType<ILoggerViewModel>().FirstOrDefault(a => a.Name == internalLogger);
        if (logger is null)
        {
            logger = this.CreateLogger(internalLogger);
            this.ContentItems.Add(logger);
            this.Selected = logger;
            await logger.AttachToAsync(new ObservableReceiver.Settings());
        }
    }

    private class UnhandledExceptionHandler(MainWindowViewModel mainWindowView)
    {
        private static readonly ILogger ExceptionLogger = Log.ForContext<UnhandledExceptionHandler>();

        public static void Register(MainWindowViewModel mainWindowView)
        {
            var handler = new UnhandledExceptionHandler(mainWindowView);
            RxApp.DefaultExceptionHandler = Observer.Create<Exception>(handler.OnException);
        }

        private void OnException(Exception exception)
        {
            mainWindowView.CreateInternalLoggerAsync().Wait();
            UnhandledExceptionHandler.ExceptionLogger.Fatal(exception, "Unhandled exception: {ExceptionType}", exception.GetType());
        }
    }
}
