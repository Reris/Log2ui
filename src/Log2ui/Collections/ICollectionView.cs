using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using Avalonia.Collections;
using Log2ui.Views;

namespace Log2ui.Collections;

public interface ICollectionView<TItem, out TCollection> : IEnumerable<TItem>, INotifyCollectionChanged
    where TCollection : IEnumerable<TItem>
{
    IEnumerable Untyped { get; }
    
    /// <summary>Gets or sets the cultural information for any operations of the view that may differ by culture, such as sorting.</summary>
    /// <returns>The culture information to use during culture-sensitive operations. </returns>
    CultureInfo Culture { get; set; }

    /// <summary>Gets the underlying collection.</summary>
    /// <returns>The underlying collection.</returns>
    TCollection SourceCollection { get; }

    /// <summary>Gets or sets a callback that is used to determine whether an item is appropriate for inclusion in the view. </summary>
    /// <returns>A method that is used to determine whether an item is appropriate for inclusion in the view.</returns>
    Func<TItem, bool>? Filter { get; set; }

    /// <summary>Gets a value that indicates whether this view supports filtering by way of the <see cref="P:System.ComponentModel.ICollectionView.Filter" /> property.</summary>
    /// <returns>true if this view supports filtering; otherwise, false.</returns>
    bool CanFilter { get; }

    /// <summary>
    /// Gets a collection of <see cref="T:System.ComponentModel.SortDescription" /> instances that describe how the items in the collection are sorted in the
    /// view.
    /// </summary>
    /// <returns>A collection of values that describe how the items in the collection are sorted in the view.</returns>
    DataGridSortDescriptionCollection SortDescriptions { get; }

    /// <summary>
    /// Gets a value that indicates whether this view supports sorting by way of the <see cref="P:System.ComponentModel.ICollectionView.SortDescriptions" />
    /// property.
    /// </summary>
    /// <returns>true if this view supports sorting; otherwise, false.</returns>
    bool CanSort { get; }

    /// <summary>
    /// Gets a value that indicates whether this view supports grouping by way of the <see cref="P:System.ComponentModel.ICollectionView.GroupDescriptions" />
    /// property.
    /// </summary>
    /// <returns>true if this view supports grouping; otherwise, false.</returns>
    bool CanGroup { get; }

    /// <summary>Gets the top-level groups.</summary>
    /// <returns>A read-only collection of the top-level groups or null if there are no groups.</returns>
    IAvaloniaReadOnlyList<DataGridGroupDescription> Groups { get; }

    /// <summary>Gets a value that indicates whether the view is empty.</summary>
    /// <returns>true if the view is empty; otherwise, false.</returns>
    bool IsEmpty { get; }

    /// <summary>Gets the current item in the view.</summary>
    /// <returns>The current item in the view or null if there is no current item.</returns>
    TItem? CurrentItem { get; }

    /// <summary>Gets the ordinal position of the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> in the view.</summary>
    /// <returns>The ordinal position of the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> in the view.</returns>
    int CurrentPosition { get; }

    /// <summary>
    /// Gets a value that indicates whether the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> of the view is beyond the end of the
    /// collection.
    /// </summary>
    /// <returns>true if the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> of the view is beyond the end of the collection; otherwise, false.</returns>
    bool IsCurrentAfterLast { get; }

    /// <summary>
    /// Gets a value that indicates whether the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> of the view is beyond the start of the
    /// collection.
    /// </summary>
    /// <returns>true if the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> of the view is beyond the start of the collection; otherwise, false.</returns>
    bool IsCurrentBeforeFirst { get; }

    /// <summary>Indicates whether the specified item belongs to this collection view. </summary>
    /// <returns>true if the item belongs to this collection view; otherwise, false.</returns>
    /// <param name="item">The object to check. </param>
    bool Contains(TItem item);

    /// <summary>Recreates the view.</summary>
    void Refresh();

    /// <summary>Enters a defer cycle that you can use to merge changes to the view and delay automatic refresh. </summary>
    /// <returns>
    /// The typical usage is to create a using scope with an implementation of this method and then include multiple view-changing calls within the scope. The
    /// implementation should delay automatic refresh until after the using scope exits.
    /// </returns>
    IDisposable DeferRefresh();

    /// <summary>Sets the first item in the view as the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" />.</summary>
    /// <returns>true if the resulting <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> is an item in the view; otherwise, false.</returns>
    bool MoveCurrentToFirst();

    /// <summary>Sets the last item in the view as the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" />.</summary>
    /// <returns>true if the resulting <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> is an item in the view; otherwise, false.</returns>
    bool MoveCurrentToLast();

    /// <summary>
    /// Sets the item after the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> in the view as the
    /// <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" />.
    /// </summary>
    /// <returns>true if the resulting <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> is an item in the view; otherwise, false.</returns>
    bool MoveCurrentToNext();

    /// <summary>
    /// Sets the item before the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> in the view to the
    /// <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" />.
    /// </summary>
    /// <returns>true if the resulting <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> is an item in the view; otherwise, false.</returns>
    bool MoveCurrentToPrevious();

    /// <summary>Sets the specified item in the view as the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" />.</summary>
    /// <returns>true if the resulting <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> is an item in the view; otherwise, false.</returns>
    /// <param name="item">The item to set as the current item.</param>
    bool MoveCurrentTo(TItem? item);

    /// <summary>Sets the item at the specified index to be the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> in the view.</summary>
    /// <returns>true if the resulting <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> is an item in the view; otherwise, false.</returns>
    /// <param name="position">The index to set the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> to.</param>
    bool MoveCurrentToPosition(int position);

    /// <summary>Occurs before the current item changes.</summary>
    event EventHandler<DataGridCurrentChangingEventArgs> CurrentChanging;

    /// <summary>Occurs after the current item has been changed.</summary>
    event EventHandler CurrentChanged;
}

public interface ICollectionView<TItem> : ICollectionView<TItem, ObservableCollection<TItem>>
{
}
