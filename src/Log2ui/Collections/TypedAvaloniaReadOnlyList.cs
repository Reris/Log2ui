using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Collections;

namespace Log2ui.Collections;

public readonly struct TypedAvaloniaReadOnlyList<TItem> : IAvaloniaReadOnlyList<TItem>
{
    private readonly IAvaloniaReadOnlyList<object> _inner;

    public TypedAvaloniaReadOnlyList(IAvaloniaReadOnlyList<object> inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        this._inner = inner;
    }

    IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => this._inner.Cast<TItem>().GetEnumerator();
    public IEnumerator GetEnumerator() => ((IEnumerable)this._inner).GetEnumerator();

    public event NotifyCollectionChangedEventHandler? CollectionChanged
    {
        add => this._inner.CollectionChanged += value;
        remove => this._inner.CollectionChanged -= value;
    }

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add => this._inner.PropertyChanged += value;
        remove => this._inner.PropertyChanged -= value;
    }

    public int Count => this._inner.Count;
    public TItem this[int index] => (TItem)this._inner[index];
}
