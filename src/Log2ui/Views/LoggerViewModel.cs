using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Log2ui.Collections;
using Log2ui.Data;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Log2ui.Receivers;
using Log2ui.Settings;
using Log2ui.Tools;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace Log2ui.Views;

public class LoggerViewModel : ViewModel, ILogMessageNotifiable, ILoggerViewModel, ILoading, ISelfRegistering
{
    private readonly IList<ReceiverSettings> _attachedReceivers = [];
    private readonly ILogManager _logManager;
    private readonly IMainDispatcher _mainDispatcher;
    private readonly IReceiverFactory _receiverFactory;
    private bool _autoScrolling;
    private ICollectionView<LogMessageItem, IList<LogMessageItem>> _logCollectionView;
    private LogLevelInfo _minLogLevel = LoggerViewModel.AllLogLevels.First(a => a.Level == LogLevel.Trace);
    private string _name;
    private bool _paused;
    private LogMessageItem? _selectedMessage;
    private string? _selectedMessageText;
    private bool _settingsOpened;

    public LoggerViewModel(
        string name,
        ICollectionView<LogMessageItem, IList<LogMessageItem>> logCollectionView,
        IMainDispatcher mainDispatcher,
        LogSearchViewModel logSearchViewModel,
        ILoggerSettingsViewModel loggerSettingsViewModel,
        IReceiverFactory receiverFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(logCollectionView);
        ArgumentNullException.ThrowIfNull(mainDispatcher);
        ArgumentNullException.ThrowIfNull(logSearchViewModel);
        ArgumentNullException.ThrowIfNull(loggerSettingsViewModel);
        ArgumentNullException.ThrowIfNull(receiverFactory);

        this._name = name;
        this._mainDispatcher = mainDispatcher;
        this._receiverFactory = receiverFactory;
        this.LogSearchViewModel = logSearchViewModel;
        this.LoggerSettingsViewModel = loggerSettingsViewModel;
        this.StyleSettings = new StyleSettingsWrapper(this.LoggerSettingsViewModel.StyleSettings);
        this.Caption = this.LoggerSettingsViewModel.LoggerSettings.Select(a => a.Name);
        this._logCollectionView = logCollectionView;
        this.RefreshFilter();
        var rootLoggerItem = LoggerItem.CreateRootLoggerItem("(root)", this._logCollectionView);
        this.TreeRoot = rootLoggerItem.TreeNode;
        this._logManager = new LogManager(rootLoggerItem);
        this.WhenAnyValue(a => a.MinLogLevel).Subscribe(_ => this.RefreshFilter()).DisposeWith(this.Disposables);
        this.WhenAnyValue(a => a.LogSearchViewModel.CurrentFilter).Subscribe(_ => this.RefreshFilter()).DisposeWith(this.Disposables);
        this.LoggerSettingsViewModel.LoggerSettings.Select(a => a.Receivers).DistinctUntilChanged().Subscribe(this.OnReceiversChanged)
            .DisposeWith(this.Disposables);
        this.Loading = this.LoadAsync();
    }

    public static IReadOnlyList<LogLevelInfo> AllLogLevels { get; } = Enum.GetValues<LogLevel>().Select(a => new LogLevelInfo(a, a.ToString())).ToArray();

    public LogSearchViewModel LogSearchViewModel { get; }
    public ILoggerSettingsViewModel LoggerSettingsViewModel { get; }

    public ICollectionView<LogMessageItem, IList<LogMessageItem>> LogCollectionView
    {
        get => this._logCollectionView;
        set => this.RaiseAndSetIfChanged(ref this._logCollectionView, value);
    }

    public bool Paused
    {
        get => this._paused;
        set => this.RaiseAndSetIfChanged(ref this._paused, value);
    }

    public bool AutoScrolling
    {
        get => this._autoScrolling;
        set => this.RaiseAndSetIfChanged(ref this._autoScrolling, value);
    }

    public LoggerTreeNode TreeRoot { get; }

    public LogMessageItem? SelectedMessage
    {
        get => this._selectedMessage;
        set => this.RaiseAndSetIfChanged(ref this._selectedMessage, value);
    }

    public string? SelectedMessageText
    {
        get => this._selectedMessageText;
        set => this.RaiseAndSetIfChanged(ref this._selectedMessageText, value);
    }

    public LogLevelInfo MinLogLevel
    {
        get => this._minLogLevel;
        set => this.RaiseAndSetIfChanged(ref this._minLogLevel, value);
    }

    public bool SettingsOpened
    {
        get => this._settingsOpened;
        set => this.RaiseAndSetIfChanged(ref this._settingsOpened, value);
    }

    public StyleSettingsWrapper StyleSettings { get; }

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
        if (this.Paused)
        {
            return;
        }

        this._mainDispatcher.InvokeAsync(() => this._logManager.ProcessLogMessage(messages));
    }

    public void Notify(LogMessage message)
    {
        if (this.Paused)
        {
            return;
        }

        this._mainDispatcher.InvokeAsync(() => this._logManager.ProcessLogMessage(message));
    }

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<ILoggerViewModel, LoggerViewModel>();
    }

    private void OnReceiversChanged(ImmutableArray<ReceiverSettings> receiverSettingsList)
    {
        var detaches = this._attachedReceivers.ExceptBy(receiverSettingsList.Select(ReceiverKey), ReceiverKey).ToArray();
        foreach (var detach in detaches)
        {
            this._receiverFactory.Detach(detach, this);
            this._attachedReceivers.Remove(detach);
        }

        var attaches = receiverSettingsList.ExceptBy(this._attachedReceivers.Select(ReceiverKey), ReceiverKey).ToArray();
        foreach (var attach in attaches)
        {
            this._receiverFactory.Attach(attach, this);
            this._attachedReceivers.Add(attach);
        }

        return;
        static (Type TypeKey, string ValueKey) ReceiverKey(ReceiverSettings a) => (a.GetType(), a.ValueKey);
    }

    private async Task LoadAsync()
    {
        var settings = await this.LoggerSettingsViewModel.LoggerSettings.GetCurrentAsync();
        this.AutoScrolling = settings.AutoScrollToLastLog;
        await Task.WhenAll(settings.Receivers.Select(this.AttachToAsync));
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
        SetEnabled(node, !node.Enabled);
        this.RefreshFilter();
        return;

        static void SetEnabled(LoggerTreeNode item, bool enabled)
        {
            item.Enabled = enabled;
            item.Logger.Enabled = enabled;
            foreach (var a in item.Logger.LogMessages)
            {
                a.Enabled = enabled;
            }

            if (UserSettings.Instance.RecursivlyEnableLoggers)
            {
                foreach (var a in item.Children)
                {
                    SetEnabled(a, enabled);
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
        this.TreeRoot.Logger.ClearAll();
    }

    public void ToggleSettingsOpened()
    {
        this.SettingsOpened = !this.SettingsOpened;
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
            item.TreeNode.Highlight = highlight;

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
