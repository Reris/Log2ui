using System;
using System.Collections.Generic;
using Log2ui.Collections;
using Log2ui.Collections.Observables;
using Log2ui.Settings;
using ReactiveUI;

namespace Log2ui.Data;

public class LoggerItem : ReactiveObject
{
    private const char LoggerSeparator = '.';

    /// <summary>
    /// A reference to the Log CollectionView associated to this Logger.
    /// </summary>
    private readonly ICollectionView<LogMessageItem, IList<LogMessageItem>> _logCollectionView;

    private readonly IValueRelay<LoggerSettings> _loggerSettings;

    /// <summary>
    /// When set the Logger and its Messages are displayed.
    /// </summary>
    private bool _enabled = true;

    private bool _hasSearchedText;

    private bool _highlight;

    /// <summary>
    /// Short Name of this Logger (used as the node name).
    /// </summary>
    private string _name;

    private string? _searchedText;

    /// <summary>
    /// Full Name (or "Path") of this Logger.
    /// </summary>
    public string FullName = string.Empty;

    private LoggerItem(
        string name,
        LoggerItem? parent,
        ICollectionView<LogMessageItem, IList<LogMessageItem>> logCollectionView,
        IValueRelay<LoggerSettings> loggerSettings)
    {
        this._name = name;
        this.Parent = parent;
        this._logCollectionView = logCollectionView;
        this._loggerSettings = loggerSettings;
    }

    /// <summary>
    /// Collection of child Logger Items, identified by their full path.
    /// </summary>
    public Dictionary<string, LoggerItem> Loggers { get; } = new();

    /// <summary>
    /// Collection of Log Messages for this Logger.
    /// </summary>
    public List<LogMessageItem> LogMessages { get; } = new();

    /// <summary>
    /// The associated Tree Node.
    /// </summary>
    public LoggerTreeNode? TreeNode { get; private set; }

    /// <summary>
    /// Parent Logger Item. Null for the Root.
    /// </summary>
    public LoggerItem? Parent { get; }


    public string Name
    {
        get => this._name;
        set
        {
            this._name = value;
            this.TreeNode.Text = this._name;
        }
    }

    public bool Enabled
    {
        get => this._enabled;
        set => this.RaiseAndSetIfChanged(ref this._enabled, value);
    }


    public bool Highlight
    {
        get => this._highlight;
        set => this.RaiseAndSetIfChanged(ref this._highlight, value);
    }

    public static LoggerItem CreateRootLoggerItem(
        string name,
        ICollectionView<LogMessageItem, IList<LogMessageItem>> logCollectionView,
        IValueRelay<LoggerSettings> loggerSettings)
    {
        var logger = new LoggerItem(name, null, logCollectionView, loggerSettings);
        logger.TreeNode = new LoggerTreeNode(name, logger);
        return logger;
    }

    private static LoggerItem CreateLoggerItem(string name, string fullName, LoggerItem parent)
    {
        ArgumentNullException.ThrowIfNull(parent);

        // Creating the logger item.
        var logger = new LoggerItem(name, parent, parent._logCollectionView, parent._loggerSettings)
        {
            FullName = fullName,
        };

        // Adding the logger as a child of the parent logger.
        parent.Loggers.Add(name, logger);

        // Creating a child logger view and saving it in the new logger.
        logger.TreeNode = parent.TreeNode.AddNew(name, logger);

        if (logger._loggerSettings.Value.LoggerTreeEnableRecursivly)
        {
            logger._enabled = parent.Enabled;
            logger.TreeNode.Enabled = parent.TreeNode.Enabled;
        }

        return logger;
    }

    public void Remove()
    {
        if (this.Parent == null)
        {
            // If root, clear all
            this.ClearAll();
            return;
        }

        this.ClearLogMessages();

        this.Parent.Loggers.Remove(this.Name);
        this.Parent.TreeNode.Remove(this.Name);
    }

    public void ClearAll()
    {
        this.ClearAllLogMessages();

        foreach (var kvp in this.Loggers)
        {
            kvp.Value.ClearAll();
        }

        this.TreeNode.Clear();
        this.Loggers.Clear();
    }

    public void ClearAllLogMessages()
    {
        this._logCollectionView.SourceCollection.Clear();
        this.LogMessages.Clear();

        foreach (var kvp in this.Loggers)
        {
            kvp.Value.ClearLogMessages();
        }
    }

    public void ClearLogMessages()
    {
        foreach (var item in this.LogMessages)
        {
            this._logCollectionView.SourceCollection.Remove(item);
        }

        this.LogMessages.Clear();

        foreach (var kvp in this.Loggers)
        {
            kvp.Value.ClearLogMessages();
        }
    }

    internal LoggerItem? GetOrCreateLogger(string loggerPath)
    {
        if (string.IsNullOrEmpty(loggerPath))
        {
            return null;
        }

        // Extract Logger Name
        var currentLoggerName = loggerPath;
        var remainingLoggerPath = string.Empty;
        var pos = loggerPath.IndexOf(LoggerItem.LoggerSeparator);
        if (pos > 0)
        {
            currentLoggerName = loggerPath.Substring(0, pos);
            remainingLoggerPath = loggerPath.Substring(pos + 1);
        }

        // Check if the Logger is in the Child Collection
        if (!this.Loggers.TryGetValue(currentLoggerName, out var logger))
        {
            // Not found here, needs to be created
            var childLoggerPath = (string.IsNullOrEmpty(this.FullName) ? "" : this.FullName + LoggerItem.LoggerSeparator) + currentLoggerName;
            logger = LoggerItem.CreateLoggerItem(currentLoggerName, childLoggerPath, this);
        }

        // Continue?
        if (!string.IsNullOrEmpty(remainingLoggerPath))
        {
            logger = logger.GetOrCreateLogger(remainingLoggerPath);
        }

        return logger;
    }

    internal void AddLogMessage(LogMessage logMessage)
    {
        var item = new LogMessageItem(this, logMessage)
        {
            Enabled = this.Enabled,
        };
        this.LogMessages.Add(item);

        // Limit the number of displayed messages if necessary
        if (this._loggerSettings.Value.MessageCycleCount > 0)
        {
            this.RemoveExtraLogMessages(this._loggerSettings.Value.MessageCycleCount);
        }

        var index = 0;
        if (this._logCollectionView.SourceCollection.Count > 0)
        {
            for (index = this._logCollectionView.SourceCollection.Count; index > 0; --index)
            {
                item.Previous = this._logCollectionView.SourceCollection[index - 1];
                if (item.Previous.Message.TimeStamp.Ticks <= item.Message.TimeStamp.Ticks)
                {
                    break;
                }
            }
        }

        // Message
        // Add it to the main list
        this._logCollectionView.SourceCollection.Insert(index, item);
    }

    private void RemoveExtraLogMessages(uint count)
    {
        var idx = 0;
        while (this.LogMessages.Count > count)
        {
            var item = this.LogMessages[idx];

            if (!item.Enabled)
            {
                count++;
                idx++;
            }

            this.RemoveLogMessage(item);
        }
    }

    private void RemoveLogMessage(LogMessageItem item)
    {
        this.LogMessages.Remove(item);

        this._logCollectionView.SourceCollection.Remove(item);
    }

    internal void SearchText(string str)
    {
        this.DoSearch(str);
    }

    private void DoSearch(string str)
    {
        this._hasSearchedText = !string.IsNullOrEmpty(str);
        this._searchedText = str;

        foreach (var (_, logger) in this.Loggers)
        {
            if (logger.Enabled)
            {
                logger.DoSearch(this._searchedText);
            }
        }
    }

    public override string ToString()
    {
        return this.Name;
    }
}
