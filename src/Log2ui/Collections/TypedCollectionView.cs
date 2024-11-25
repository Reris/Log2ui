using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Avalonia.Collections;
using Log2ui.Views;

namespace Log2ui.Collections;

public class TypedCollectionView<TItem, TCollection> : IEditableCollectionView<TItem, TCollection>
    where TCollection : IEnumerable<TItem>
{
    private Func<TItem, bool>? _filter;

    public TypedCollectionView(TCollection source)
    {
        ArgumentNullException.ThrowIfNull(source);

        this.Untyped = new DataGridCollectionView(source);
    }

    public DataGridCollectionView Untyped { get; }

    IEnumerable ICollectionView<TItem, TCollection>.Untyped => this.Untyped;

    IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => this.Untyped.Cast<TItem>().GetEnumerator();
    public IEnumerator GetEnumerator() => ((IEnumerable)this.Untyped).GetEnumerator();

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
        get => this._filter;
        set
        {
            this._filter = value;
            this.Untyped.Filter = value is null ? null : a => value((TItem)a);
        }
    }

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
    public bool Contains(TItem item) => this.Untyped.Contains(item);
    public void Refresh() => this.Untyped.Refresh();
    public IDisposable DeferRefresh() => this.Untyped.DeferRefresh();
    public bool MoveCurrentToFirst() => this.Untyped.MoveCurrentToFirst();
    public bool MoveCurrentToLast() => this.Untyped.MoveCurrentToLast();
    public bool MoveCurrentToNext() => this.Untyped.MoveCurrentToNext();
    public bool MoveCurrentToPrevious() => this.Untyped.MoveCurrentToPrevious();
    public bool MoveCurrentTo(TItem? item) => this.Untyped.MoveCurrentTo(item);
    public bool MoveCurrentToPosition(int position) => this.Untyped.MoveCurrentToPosition(position);

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
    public TItem AddNew() => (TItem)this.Untyped.AddNew();
    public void CommitNew() => this.Untyped.CommitNew();
    public void CancelNew() => this.Untyped.CancelNew();
    public void RemoveAt(int index) => this.Untyped.RemoveAt(index);
    public void Remove(TItem item) => this.Untyped.Remove(item);
    public void EditItem(TItem item) => this.Untyped.EditItem(item);
    public void CommitEdit() => this.Untyped.CommitEdit();
    public void CancelEdit() => this.Untyped.CancelEdit();
}

public class TypedCollectionView<TItem> : TypedCollectionView<TItem, ObservableCollection<TItem>>, IEditableCollectionView<TItem>
{
    public TypedCollectionView()
        : this(new ObservableCollection<TItem>())
    {
    }

    public TypedCollectionView(ObservableCollection<TItem> source)
        : base(source)
    {
    }
}
