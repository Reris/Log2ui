using System;
using System.Collections.Generic;
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

public class LoggerViewModel : ViewModel, ILogMessageNotifiable, ILoggerViewModel, IDisposable, ILoading, ISelfRegistering
{
    private readonly ILogManager _logManager;
    private readonly IMainDispatcher _mainDispatcher;
    private readonly IList<IReceiver> _receivers = [];
    private bool _autoScrolling;
    private ICollectionView<LogMessageItem, IList<LogMessageItem>> _logCollectionView;
    private LogLevelInfo _minLogLevel = LogLevels.Of(LogLevel.Trace);
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
        LoggerSettingsViewModel loggerSettingsViewModel)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(logCollectionView);
        ArgumentNullException.ThrowIfNull(mainDispatcher);
        ArgumentNullException.ThrowIfNull(logSearchViewModel);
        ArgumentNullException.ThrowIfNull(loggerSettingsViewModel);

        this._name = name;
        this._mainDispatcher = mainDispatcher;
        this.LogSearchViewModel = logSearchViewModel;
        this.LoggerSettingsViewModel = loggerSettingsViewModel;
        this.StyleSettings = new StyleSettingsWrapper(this.LoggerSettingsViewModel.StyleSettings);
        this.Caption = this.LoggerSettingsViewModel.LoggerSettings.Select(a => a.Name);
        this._logCollectionView = logCollectionView;
        this.RefreshFilter();
        var rootLoggerItem = LoggerItem.CreateRootLoggerItem("(root)", this._logCollectionView);
        this.TreeRoot = rootLoggerItem.TreeNode;
        this._logManager = new LogManager(rootLoggerItem);
        this.WhenAnyValue(a => a.MinLogLevel).Subscribe(_ => this.RefreshFilter());
        this.WhenAnyValue(a => a.LogSearchViewModel.CurrentFilter).Subscribe(_ => this.RefreshFilter());
        this.Loading = this.LoadAsync();
    }

    public LogSearchViewModel LogSearchViewModel { get; }
    public LoggerSettingsViewModel LoggerSettingsViewModel { get; }

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

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public Task Loading { get; }

    public string Name
    {
        get => this._name;
        set => this.RaiseAndSetIfChanged(ref this._name, value);
    }

    public IObservable<string> Caption { get; }

    public void AttachTo(IReceiver receiver)
    {
        receiver.Attach(this);
        this._receivers.Add(receiver);
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

    private async Task LoadAsync()
    {
        var settings = await this.LoggerSettingsViewModel.LoggerSettings.GetCurrentAsync();
        this.AutoScrolling = settings.AutoScrollToLastLog;
    }

    public void UpdateSelectedMessageText()
    {
        this.SelectedMessageText = this.SelectedMessage?.Message.Message;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        foreach (var receiver in this._receivers)
        {
            receiver.Detach(this);
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
                   && a.Message.Level >= this.MinLogLevel
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
    }
}
