using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Log2ui.Collections;
using Log2ui.Collections.Observables;
using Log2ui.Data;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Log2ui.Receivers;
using Log2ui.Settings;
using Log2ui.Tools;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace Log2ui.Views;

public class LoggerViewModel : ViewModel, ILogMessageNotifiable, ILoggerViewModel, ILoading, IClosed, ISelfRegistering
{
    private readonly IList<ReceiverSettings> _attachedReceivers = [];
    private readonly IMainDispatcher _mainDispatcher;
    private readonly IReceiverFactory _receiverFactory;
    private ICollectionView<LogMessageItem, IList<LogMessageItem>> _logCollectionView;
    private ValueRelay<LoggerSettings>? _loggerSettingsRelay;
    private ILogManager? _logManager;
    private string _name;

    public LoggerViewModel(
        string name,
        ICollectionView<LogMessageItem, IList<LogMessageItem>> logCollectionView,
        IMainDispatcher mainDispatcher,
        ILogSearchViewModel logSearchViewModel,
        ILoggerSettingsViewModel loggerSettingsViewModel,
        IReceiverManagerViewModel receiverManagerViewModel,
        IReceiverFactory receiverFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(logCollectionView);
        ArgumentNullException.ThrowIfNull(mainDispatcher);
        ArgumentNullException.ThrowIfNull(logSearchViewModel);
        ArgumentNullException.ThrowIfNull(loggerSettingsViewModel);
        ArgumentNullException.ThrowIfNull(receiverManagerViewModel);
        ArgumentNullException.ThrowIfNull(receiverFactory);

        this._name = name;
        this._mainDispatcher = mainDispatcher;
        this._receiverFactory = receiverFactory;
        this.LogSearchViewModel = logSearchViewModel;
        this.LoggerSettingsViewModel = loggerSettingsViewModel;
        this.ReceiverManagerViewModel = receiverManagerViewModel;
        this.LoggerSettings = new LoggerSettingsWrapper(this.LoggerSettingsViewModel.LoggerSettings);
        this.StyleSettings = new StyleSettingsWrapper(this.LoggerSettingsViewModel.StyleSettings);
        this.Caption = this.LoggerSettingsViewModel.LoggerSettings.Select(a => a.Name);
        this._logCollectionView = logCollectionView;
        this.RefreshFilter();
        this.WhenAnyValue(a => a.MinLogLevel).Subscribe(_ => this.RefreshFilter()).DisposeWith(this.Disposables);
        this.WhenAnyValue(a => a.LogSearchViewModel.CurrentFilter).Subscribe(_ => this.RefreshFilter()).DisposeWith(this.Disposables);
        this.AllReceiverSettings.CombineLatest(this.LoggerSettings.ReceiverKeys).DistinctUntilChanged().Subscribe(this.OnReceiversChanged)
            .DisposeWith(this.Disposables);
        this.Loading = this.LoadAsync();
    }

    public static IReadOnlyList<LogLevelInfo> AllLogLevels { get; } = Enum.GetValues<LogLevel>().Select(a => new LogLevelInfo(a, a.ToString())).ToArray();

    public ILogSearchViewModel LogSearchViewModel { get; }
    public ILoggerSettingsViewModel LoggerSettingsViewModel { get; }
    public IReceiverManagerViewModel ReceiverManagerViewModel { get; }

    public ICollectionView<LogMessageItem, IList<LogMessageItem>> LogCollectionView
    {
        get => this._logCollectionView;
        set => this.RaiseAndSetIfChanged(ref this._logCollectionView, value);
    }

    public bool Paused
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool AutoScrolling
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public LoggerTreeNode? LoggerTreeRoot
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public IReadOnlyList<LoggerTreeNode> LoggerTree
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    public LogMessageItem? SelectedMessage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? SelectedMessageText
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public LogLevelInfo MinLogLevel
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = LoggerViewModel.AllLogLevels.First(a => a.Level == LogLevel.Trace);

    public bool SettingsOpened
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool ReceiversOpened
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public LoggerSettingsWrapper LoggerSettings { get; }
    public StyleSettingsWrapper StyleSettings { get; }
    public IObservable<AllReceiverSettings> AllReceiverSettings => this.LoggerSettingsViewModel.AllReceiverSettings;

    public async Task ClosedAsync()
    {
        await this.LoggerSettingsViewModel.RemoveAsync();
    }

    public Task Loading { get; }

    public string Name
    {
        get => this._name;
        set => this.RaiseAndSetIfChanged(ref this._name, value);
    }

    public IObservable<string> Caption { get; }

    public async Task<bool> AttachToAsync(ReceiverSettings receiverSettings)
    {
        return await this.LoggerSettingsViewModel.AddReceiverAsync(receiverSettings);
    }


    public void Notify(IReadOnlyList<LogMessage> messages)
    {
        if (this.Paused || this._logManager is null)
        {
            return;
        }

        this._mainDispatcher.InvokeAsync(() => this._logManager.ProcessLogMessage(messages));
    }

    public void Notify(LogMessage message)
    {
        if (this.Paused || this._logManager is null)
        {
            return;
        }

        this._mainDispatcher.InvokeAsync(() => this._logManager.ProcessLogMessage(message));
    }

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<ILoggerViewModel, LoggerViewModel>();
    }


    private void OnReceiversChanged((AllReceiverSettings First, EquatableArray<string> Second) next)
    {
        var (all, chosen) = next;
        if (chosen.Count == 0 && this._attachedReceivers.Count == 0)
        {
            return;
        }

        var detaches = this._attachedReceivers.ExceptBy(all.Receivers.Select(a => a.Key), a => a.Key)
                           .Concat(this._attachedReceivers.ExceptBy(chosen, a => a.Key))
                           .Distinct()
                           .ToArray();
        foreach (var detach in detaches)
        {
            this._receiverFactory.Detach(detach, this);
            this._attachedReceivers.Remove(detach);
        }

        var attaches = all.Receivers.ExceptBy(this._attachedReceivers.Select(a => a.Key), a => a.Key)
                          .Where(a => chosen.Contains(a.Key))
                          .ToArray();
        foreach (var attach in attaches)
        {
            this._receiverFactory.Attach(attach, this);
            this._attachedReceivers.Add(attach);
        }
    }

    private async Task LoadAsync()
    {
        var settings = await this.LoggerSettingsViewModel.LoggerSettings.GetCurrentAsync();
        this.AutoScrolling = settings.AutoScrollToLastLog;
        this.MinLogLevel = LoggerViewModel.AllLogLevels.FirstOrDefault(a => a.Level == settings.DefaultMinLogLevel) ?? this.MinLogLevel;

        this.BindLogger(settings);

        var receivers = await this.AllReceiverSettings.GetCurrentAsync().SelectAsync(a => a.Receivers.Where(b => settings.ReceiverKeys.Contains(b.Key)));
        await Task.WhenAll(receivers.Select(this.AttachToAsync));
        await this.LoggerSettingsViewModel.SaveAsync();
    }

    private void BindLogger(NamedLoggerSettings settings)
    {
        var relay = new ValueRelay<LoggerSettings>(settings);
        this._loggerSettingsRelay = relay;
        this.LoggerSettingsViewModel.LoggerSettings.Subscribe(a => relay.Value = a).DisposeWith(this.Disposables);
        var rootLoggerItem = LoggerItem.CreateRootLoggerItem("(root)", this._logCollectionView, relay);
        this.LoggerTreeRoot = rootLoggerItem.TreeNode ?? throw new NotInitializedException(nameof(rootLoggerItem.TreeNode));
        this.LoggerTree = this.LoggerTreeRoot.Children;
        this._logManager = new LogManager(rootLoggerItem);
    }

    public void UpdateSelectedMessageText()
    {
        this.SelectedMessageText = this.SelectedMessage?.Message.Message;
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        foreach (var attached in this._attachedReceivers)
        {
            this._receiverFactory.Detach(attached, this);
        }
    }

    public void ToggleTreeItem(LoggerTreeNode node)
    {
        SetEnabled(this, node, !node.Enabled);
        this.RefreshFilter();
        return;

        static void SetEnabled(LoggerViewModel loggerViewModel, LoggerTreeNode item, bool enabled)
        {
            item.Enabled = enabled;
            item.Logger.Enabled = enabled;
            foreach (var a in item.Logger.LogMessages)
            {
                a.Enabled = enabled;
            }

            if (loggerViewModel._loggerSettingsRelay?.Value.LoggerTreeEnableRecursivly is true)
            {
                foreach (var a in item.Children)
                {
                    SetEnabled(loggerViewModel, a, enabled);
                }
            }
        }
    }

    public void TogglePaused()
    {
        this.Paused = !this.Paused;
    }

    public void ToggleAutoScrolling()
    {
        this.AutoScrolling = !this.AutoScrolling;
    }

    public void ClearMessages()
    {
        this._logCollectionView.SourceCollection.Clear();
    }

    public void ClearAll()
    {
        this.LoggerTreeRoot?.Logger.ClearAll();
    }

    public void ToggleSettingsOpened()
    {
        this.SettingsOpened = !this.SettingsOpened;
    }

    public void ToggleReceiversOpened()
    {
        this.ReceiversOpened = !this.ReceiversOpened;
    }

    private void RefreshFilter()
    {
        this._logCollectionView.Filter
            = a => a.Enabled
                   && a.Message.Level >= this.MinLogLevel.Level
                   && this.LogSearchViewModel.CurrentFilter?.Invoke(a) != false;
    }

    public void Highlight(LoggerItem logger)
    {
        SetHighlight(logger, !logger.Highlight);
        return;

        static void SetHighlight(LoggerItem item, bool highlight)
        {
            item.Highlight = highlight;
            item.TreeNode!.Highlight = highlight;

            if (item.Parent is not null)
            {
                SetHighlight(item.Parent, highlight);
            }
        }
    }

    public void HighlightMessages(LoggerItem logger, bool highlight)
    {
        foreach (var a in logger.LogMessages)
        {
            a.Highlight = highlight;
        }
    }

    public class LoggerSettingsWrapper(IObservable<NamedLoggerSettings> loggerSettings)
    {
        public IObservable<EquatableArray<string>> ReceiverKeys { get; } = loggerSettings.Select(a => a.ReceiverKeys);
        public IObservable<bool> ShowLoggerTree { get; } = loggerSettings.Select(a => a.ShowLoggerTree);
        public IObservable<bool> ShowMsgDetails { get; } = loggerSettings.Select(a => a.ShowMsgDetails);
        public IObservable<string> TimeStampFormatString { get; } = loggerSettings.Select(a => $"{{0:{a.TimeStampFormatString}}}");
    }

    public class StyleSettingsWrapper(IObservable<LoggerStyleSettings> styleSettings)
    {
        public IObservable<Color> LogListBackColor { get; } = styleSettings.Select(a => a.LogListBackColor);
        public IObservable<Color> LogMessageBackColor { get; } = styleSettings.Select(a => a.LogMessageBackColor);
        public IObservable<Color> TraceLevelColor { get; } = styleSettings.Select(a => a.TraceLevelColor);
        public IObservable<Color> DebugLevelColor { get; } = styleSettings.Select(a => a.DebugLevelColor);
        public IObservable<Color> InfoLevelColor { get; } = styleSettings.Select(a => a.InfoLevelColor);
        public IObservable<Color> WarnLevelColor { get; } = styleSettings.Select(a => a.WarnLevelColor);
        public IObservable<Color> ErrorLevelColor { get; } = styleSettings.Select(a => a.ErrorLevelColor);
        public IObservable<Color> FatalLevelColor { get; } = styleSettings.Select(a => a.FatalLevelColor);
    }
}
