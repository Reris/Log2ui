using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Log2ui.Receivers;
using Log2ui.Settings;
using Log2ui.Tools;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Serilog;

namespace Log2ui.Views;

public class MainWindowViewModel : ViewModel, ISelfRegistering
{
    private static readonly ILogger Logger = Log.ForContext<MainWindowViewModel>();

    private readonly IReceiver _internalLog;
    private readonly IMainDispatcher _mainDispatcher;
    private readonly ISettingsService _settingsService;
    private readonly IViewModelFactory _viewModelFactory;
    private ICaptionedViewModel? _selected;

    public MainWindowViewModel(IMainDispatcher mainDispatcher, IViewModelFactory viewModelFactory, ISettingsService settingsService, IReceiver internalLog)
    {
        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(this.OnException);

        this._mainDispatcher = mainDispatcher;
        this._viewModelFactory = viewModelFactory;
        this._settingsService = settingsService;
        this._internalLog = internalLog;
        this.CreateLoggerFunc = () => this.CreateLogger();
        this.UserSettingsCommand = ReactiveCommand.Create(this.OpenGlobalSettings);
        this.ContentItems.CollectionChanged += this.ContentItemsOnCollectionChanged;
        this.AddLogger();
        this.CreateInternalLogger();
    }

    public ObservableCollection<ICaptionedViewModel> ContentItems { get; } = [];

    public ICaptionedViewModel? Selected
    {
        get => this._selected;
        set => this.RaiseAndSetIfChanged(ref this._selected, value);
    }

    public ReactiveCommand<Unit, Unit> UserSettingsCommand { get; }
    public Func<ILoggerViewModel> CreateLoggerFunc { get; }

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<MainWindowViewModel>();
    }

    public async Task LoadAsync()
    {
        await Task.Factory.AwaitInPool();
    }

    private ILoggerViewModel CreateLogger(string? withName = null)
    {
        withName ??= this.CreateLoggerName();
        return this._viewModelFactory.Create<ILoggerViewModel>(withName);
    }

    private void OpenGlobalSettings()
    {
        if (this.ContentItems.OfType<AppSettingsViewModel>().Any())
        {
            return;
        }

        var settingsVm = this._viewModelFactory.Create<AppSettingsViewModel>(this._settingsService.AppSettings);
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
                (viewModel as IDisposable)?.Dispose();
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
                    async () =>
                    {
                        await Task.Yield();
                        this.AddLogger();
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

    private void AddLogger()
    {
        var logger = this.CreateLogger();
        logger.AttachTo(new TcpReceiver());
        this.ContentItems.Add(logger);
        this.Selected = logger;
    }

    private void OnException(Exception exception)
    {
        this.CreateInternalLogger();
        MainWindowViewModel.Logger.Fatal(exception, "Unhandled exception");
    }

    private void CreateInternalLogger()
    {
        const string internalLogger = "Log2ui-Log";

        var logger = this.ContentItems.OfType<ILoggerViewModel>().FirstOrDefault(a => a.Name == internalLogger);
        if (logger is null)
        {
            logger = this.CreateLogger(internalLogger);
            logger.AttachTo(this._internalLog);
            this.ContentItems.Add(logger);
            this.Selected = logger;
        }
    }
}
