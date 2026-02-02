using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Avalonia.Collections;

namespace Log2ui.Collections;

public class TypedCollectionView<TItem, TCollection> : IEditableCollectionView<TItem, TCollection>
    where TCollection : IEnumerable<TItem>
{
    public TypedCollectionView(TCollection source)
    {
        ArgumentNullException.ThrowIfNull(source);

        this.Untyped = new DataGridCollectionView(source);
    }

    public DataGridCollectionView Untyped { get; }

    IEnumerable ICollectionView<TItem, TCollection>.Untyped => this.Untyped;

    IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator()
    {
        return this.Untyped.Cast<TItem>().GetEnumerator();
    }

    public IEnumerator GetEnumerator()
    {
        return ((IEnumerable)this.Untyped).GetEnumerator();
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged
    {
        add => ((INotifyCollectionChanged)this.Untyped).CollectionChanged += value;
        remove => ((INotifyCollectionChanged)this.Untyped).CollectionChanged -= value;
    }

    public CultureInfo Culture
    {
        get => this.Untyped.Culture;
        set => this.Untyped.Culture = value;
    }

    public TCollection SourceCollection => (TCollection)this.Untyped.SourceCollection;

    public Func<TItem, bool>? Filter
    {
        get;
        set
        {
            field = value;
            this.Untyped.Filter = value is null ? null : a => value((TItem)a);
        }
    }

    public int Count => this.Untyped.Count;
    public bool CanFilter => this.Untyped.CanFilter;
    public DataGridSortDescriptionCollection SortDescriptions => this.Untyped.SortDescriptions;
    public bool CanSort => this.Untyped.CanSort;
    public bool CanGroup => this.Untyped.CanGroup;
    public IAvaloniaReadOnlyList<DataGridGroupDescription> Groups => new TypedAvaloniaReadOnlyList<DataGridGroupDescription>(this.Untyped.Groups);
    public bool IsEmpty => this.Untyped.IsEmpty;
    public TItem? CurrentItem => (TItem?)this.Untyped.CurrentItem;
    public int CurrentPosition => this.Untyped.CurrentPosition;
    public bool IsCurrentAfterLast => this.Untyped.IsCurrentAfterLast;
    public bool IsCurrentBeforeFirst => this.Untyped.IsCurrentBeforeFirst;

    public bool Contains(TItem item)
    {
        return this.Untyped.Contains(item);
    }

    public void Refresh()
    {
        this.Untyped.Refresh();
    }

    public IDisposable DeferRefresh()
    {
        return this.Untyped.DeferRefresh();
    }

    public bool MoveCurrentToFirst()
    {
        return this.Untyped.MoveCurrentToFirst();
    }

    public bool MoveCurrentToLast()
    {
        return this.Untyped.MoveCurrentToLast();
    }

    public bool MoveCurrentToNext()
    {
        return this.Untyped.MoveCurrentToNext();
    }

    public bool MoveCurrentToPrevious()
    {
        return this.Untyped.MoveCurrentToPrevious();
    }

    public bool MoveCurrentTo(TItem? item)
    {
        return this.Untyped.MoveCurrentTo(item);
    }

    public bool MoveCurrentToPosition(int position)
    {
        return this.Untyped.MoveCurrentToPosition(position);
    }

    public event EventHandler<DataGridCurrentChangingEventArgs>? CurrentChanging
    {
        add => this.Untyped.CurrentChanging += value;
        remove => this.Untyped.CurrentChanging -= value;
    }

    public event EventHandler? CurrentChanged
    {
        add => this.Untyped.CurrentChanged += value;
        remove => this.Untyped.CurrentChanged -= value;
    }

    public bool CanAddNew => this.Untyped.CanAddNew;
    public bool IsAddingNew => this.Untyped.IsAddingNew;
    public TItem? CurrentAddItem => (TItem?)this.Untyped.CurrentAddItem;
    public bool CanRemove => this.Untyped.CanRemove;
    public bool CanCancelEdit => this.Untyped.CanCancelEdit;
    public bool IsEditingItem => this.Untyped.IsEditingItem;
    public object CurrentEditItem => this.Untyped.CurrentEditItem;

    public TItem AddNew()
    {
        return (TItem)this.Untyped.AddNew();
    }

    public void CommitNew()
    {
        this.Untyped.CommitNew();
    }

    public void CancelNew()
    {
        this.Untyped.CancelNew();
    }

    public void RemoveAt(int index)
    {
        this.Untyped.RemoveAt(index);
    }

    public void Remove(TItem item)
    {
        this.Untyped.Remove(item);
    }

    public void EditItem(TItem item)
    {
        this.Untyped.EditItem(item);
    }

    public void CommitEdit()
    {
        this.Untyped.CommitEdit();
    }

    public void CancelEdit()
    {
        this.Untyped.CancelEdit();
    }
}

public class TypedCollectionView<TItem>(ObservableCollection<TItem> source) : TypedCollectionView<TItem, ObservableCollection<TItem>>(source),
                                                                              IEditableCollectionView<TItem>
{
    public TypedCollectionView()
        : this(new ObservableCollection<TItem>())
    {
    }
}
