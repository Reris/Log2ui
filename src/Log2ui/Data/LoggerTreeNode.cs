using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Log2ui.Collections;
using ReactiveUI;

namespace Log2ui.Data;

public class LoggerTreeNode : ReactiveObject
{
    private readonly ObservableCollection<LoggerTreeNode> _children = new();
    private bool _enabled = true;
    private bool _highlight;
    private string _text;

    public LoggerTreeNode(string text, LoggerItem logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        this._text = text;
        this.Logger = logger;
    }

    public LoggerItem Logger { get; }

    public string Text
    {
        get => this._text;
        set => this.RaiseAndSetIfChanged(ref this._text, value);
    }

    public bool Highlight
    {
        get => this._highlight;
        set => this.RaiseAndSetIfChanged(ref this._highlight, value);
    }

    public bool Enabled
    {
        get => this._enabled;
        set => this.RaiseAndSetIfChanged(ref this._enabled, value);
    }

    public IReadOnlyList<LoggerTreeNode> Children => this._children;

    public void Clear()
    {
        this._children.Clear();
    }

    public LoggerTreeNode AddNew(string text, LoggerItem logger)
    {
        var result = new LoggerTreeNode(text, logger);
        this._children.Add(result);
        return result;
    }

    public void Remove(string text)
    {
        if (this._children.TryGetFirstIndex(a => a.Text == text, out var index))
        {
            this._children.RemoveAt(index);
        }
    }
}
