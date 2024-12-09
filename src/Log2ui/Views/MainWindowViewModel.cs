using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Log2ui.Receivers;
using Log2ui.Tools;
using ReactiveUI;
using Serilog;

namespace Log2ui.Views;

public class MainWindowViewModel : ViewModel
{
    private static readonly ILogger Logger = Log.ForContext<MainWindowViewModel>();

    private readonly IReceiver _internalLog;
    private readonly IMainDispatcher _mainDispatcher;
    private readonly IViewModelFactory _viewModelFactory;
    private ILoggerViewModel? _selected;

    public MainWindowViewModel(IMainDispatcher mainDispatcher, IViewModelFactory viewModelFactory, IReceiver internalLog)
    {
        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(this.OnException);

        this._mainDispatcher = mainDispatcher;
        this._viewModelFactory = viewModelFactory;
        this._internalLog = internalLog;
        this.CreateLoggerFunc = () => this.CreateLogger();
        this.UserSettingsCommand = ReactiveCommand.Create(this.OpenGlobalSettings);
        this.Loggers.CollectionChanged += this.LoggersOnCollectionChanged;
        this.AddLogger();
    }

    public ObservableCollection<ILoggerViewModel> Loggers { get; } = [];

    public ILoggerViewModel? Selected
    {
        get => this._selected;
        set => this.RaiseAndSetIfChanged(ref this._selected, value);
    }

    public ReactiveCommand<Unit, Unit> UserSettingsCommand { get; }
    public Func<ILoggerViewModel> CreateLoggerFunc { get; }

    private ILoggerViewModel CreateLogger(string? withName = null)
    {
        withName ??= this.CreateLoggerName();
        return this._viewModelFactory.Create<ILoggerViewModel>(withName);
    }

    private void OpenGlobalSettings()
    {
        throw new NotImplementedException();
    }

    private void LoggersOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Remove:
            {
                var logger = e.OldItems!.Cast<LoggerViewModel>().Single();
                logger.Dispose();

                if (this.Loggers.Count == 0)
                {
                    this._mainDispatcher.InvokeAsync(
                        async () =>
                        {
                            await Task.Yield();
                            this.AddLogger();
                        });
                }

                break;
            }
        }
    }

    private string CreateLoggerName()
    {
        for (var i = 1; i < 10000; i++)
        {
            var name = $"Log{i}";
            if (this.Loggers.All(a => a.Name != name))
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
        this.Loggers.Add(logger);
        this.Selected = logger;
    }

    private void OnException(Exception exception)
    {
        this.CreateInternalLogger();
        MainWindowViewModel.Logger.Fatal(exception, "Unhandled exception");
    }

    private void CreateInternalLogger()
    {
        const string internalLogger = "Log2Ui-Log";

        var logger = this.Loggers.FirstOrDefault(a => a.Name == internalLogger);
        if (logger is null)
        {
            logger = this.CreateLogger(internalLogger);
            logger.AttachTo(this._internalLog);
            this.Loggers.Add(logger);
            this.Selected = logger;
        }
    }
}
