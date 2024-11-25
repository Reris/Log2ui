using System;
using System.Collections.Generic;
using Log2ui.Collections;
using Log2ui.Data;
using Log2ui.Helpers;
using Log2ui.Receivers;
using Log2ui.Settings;
using ReactiveUI;

namespace Log2ui.Views;

public class LoggerViewModel : ViewModel, ILogMessageNotifiable, IDisposable
{
    private readonly ILogManager _logManager;
    private readonly IMainDispatcher _mainDispatcher;
    private bool _autoScrolling = UserSettings.Instance.AutoScrollToLastLog;
    private ICollectionView<LogMessageItem, IList<LogMessageItem>> _logCollectionView;
    private LogLevelInfo _minLogLevel = LogLevels.Of(LogLevel.Trace);
    private string _name;
    private bool _paused;
    private LogMessageItem? _selectedMessage;
    private readonly TcpReceiver _testTcpReceiver;

    public LoggerViewModel(
        string name,
        ICollectionView<LogMessageItem, IList<LogMessageItem>> logCollectionView,
        IMainDispatcher mainDispatcher,
        LogSearchViewModel logSearchViewModel)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(logCollectionView);
        ArgumentNullException.ThrowIfNull(mainDispatcher);
        ArgumentNullException.ThrowIfNull(logSearchViewModel);

        this._name = name;
        this._mainDispatcher = mainDispatcher;
        this.LogSearchViewModel = logSearchViewModel;
        this._logCollectionView = logCollectionView;
        this.RefreshFilter();
        var rootLoggerItem = LoggerItem.CreateRootLoggerItem("(root)", this._logCollectionView);
        this.TreeRoot = rootLoggerItem.TreeNode;
        this._logManager = new LogManager(rootLoggerItem);
        this.WhenAnyValue(a => a.MinLogLevel).Subscribe(_ => this.RefreshFilter());
        this.WhenAnyValue(a => a.LogSearchViewModel.CurrentFilter).Subscribe(_ => this.RefreshFilter());

        this._testTcpReceiver = new TcpReceiver();
        this._testTcpReceiver.Attach(this);
        try
        {
            this._testTcpReceiver.Initialize();
        }
        catch
        {
        }
    }

    public LogSearchViewModel LogSearchViewModel { get; }

    public ICollectionView<LogMessageItem, IList<LogMessageItem>> LogCollectionView
    {
        get => this._logCollectionView;
        set => this.RaiseAndSetIfChanged(ref this._logCollectionView, value);
    }

    public string Name
    {
        get => this._name;
        set => this.RaiseAndSetIfChanged(ref this._name, value);
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

    public LogLevelInfo MinLogLevel
    {
        get => this._minLogLevel;
        set => this.RaiseAndSetIfChanged(ref this._minLogLevel, value);
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        this._testTcpReceiver.Detach(this);
    }

    public void Notify(LogMessage[] messages)
    {
        if (this.Paused) return;

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
}
