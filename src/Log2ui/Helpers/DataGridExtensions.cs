using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Log2ui.Collections;

namespace Log2ui.Helpers;

public static class DataGridExtensions
{
    static DataGridExtensions()
    {
        DataGridExtensions.AutoScrollProperty.Changed.Subscribe(
            x => DataGridExtensions.OnAutoScrollChanged(x.Sender, x.NewValue.GetValueOrDefault()));
        DataGrid.ItemsSourceProperty.Changed.Subscribe(
            x => DataGridExtensions.OnItemsSourceChanged(x.Sender, x.OldValue.GetValueOrDefault(), x.NewValue.GetValueOrDefault()));
    }

    public static void RegisterUnselector(this DataGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

        grid.KeyUp += OnGridOnKeyUp;

        grid.AddHandler(InputElement.KeyUpEvent, OnGridOnKeyUp);

        var beginDeselectIndex = -1;
        grid.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        grid.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
        return;

        void OnGridOnKeyUp(object? _, KeyEventArgs e)
        {
            if (e is not { Handled: false, Key: Key.Escape } || grid.SelectedIndex == -1)
            {
                return;
            }

            grid.SelectedIndex = -1;
            e.Handled = true;
        }

        void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.Handled || e.ClickCount != 1 || grid.SelectedIndex == -1)
            {
                return;
            }

            beginDeselectIndex = grid.SelectedIndex;
        }

        void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e is not { Handled: false, InitialPressMouseButton: MouseButton.Left } || grid.SelectedIndex != beginDeselectIndex)
            {
                return;
            }

            beginDeselectIndex = -1;
            grid.SelectedIndex = -1;
            e.Handled = true;
        }
    }

    public static ClassTriggerBuilder<TStart> BuildClassTrigger<TStart>(this DataGrid grid)
        where TStart : INotifyPropertyChanged
        => new(grid, Array.Empty<Func<object, INotifyPropertyChanged?>>());

    public readonly struct ClassTriggerBuilder<T>
        where T : INotifyPropertyChanged
    {
        private readonly DataGrid _grid;
        private readonly IEnumerable<Func<object, INotifyPropertyChanged?>> _path;

        public ClassTriggerBuilder(DataGrid grid, IEnumerable<Func<object, INotifyPropertyChanged?>> path)
        {
            ArgumentNullException.ThrowIfNull(grid);
            ArgumentNullException.ThrowIfNull(path);

            this._grid = grid;
            this._path = path;
        }

        public ClassTriggerBuilder<TNext> On<TNext>(Func<T, TNext?> property)
            where TNext : INotifyPropertyChanged
            => new(this._grid, this._path.Append(a => property((T)a)));

        public IDisposable Attach(Predicate<T> predicate, string classNames)
        {
            var classArray = classNames.Split(" ");
            var listener = new GridListener<T>(this._grid, this._path, a => predicate(a) ? classArray : null);
            listener.Attach();
            return listener;
        }
    }

    private class GridListener<T> : IDisposable
        where T : INotifyPropertyChanged
    {
        private readonly Func<T, string[]?> _classDeterminator;
        private readonly IEnumerable<Func<object, INotifyPropertyChanged?>> _path;
        private readonly List<RowListener<T>?> _rowListeners = new(64);

        public GridListener(DataGrid grid, IEnumerable<Func<object, INotifyPropertyChanged?>> path, Func<T, string[]?> classDeterminator)
        {
            ArgumentNullException.ThrowIfNull(grid);
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(classDeterminator);

            this.Grid = grid;
            this._path = path;
            this._classDeterminator = classDeterminator;
        }

        private DataGrid Grid { get; }

        public void Dispose()
        {
            this.Grid.LoadingRow -= this.EnsureAttachedRowListener;
            this.Grid.Unloaded -= this.DetachAllRowListeners;

            foreach (var listener in this._rowListeners.NotNull())
            {
                listener.Detach();
            }

            this._rowListeners.Clear();
        }

        public void Attach()
        {
            this.Grid.LoadingRow += this.EnsureAttachedRowListener;
            this.Grid.Unloaded += this.DetachAllRowListeners;
        }

        private void EnsureAttachedRowListener(object? sender, DataGridRowEventArgs e)
        {
            if (this._rowListeners.Any(a => e.Row == a?.Row))
            {
                return;
            }

            var path = this._path.Prepend(a => ((DataGridRow)a).DataContext as INotifyPropertyChanged).ToArray();
            var listener = new RowListener<T>(e.Row, path, this._classDeterminator);
            this.AddOrSet(listener);
            listener.Attach();
        }

        private void AddOrSet(RowListener<T> listener)
        {
            if (this._rowListeners.TryGetFirstIndex(a => a is null, out var free))
            {
                this._rowListeners[free] = listener;
            }
            else
            {
                this._rowListeners.Add(listener);
            }
        }

        private void DetachAllRowListeners(object? sender, EventArgs e)
        {
            foreach (var rowListener in this._rowListeners.NotNull())
            {
                rowListener.Detach();
            }

            this._rowListeners.Clear();
        }
    }

    private class RowListener<T>
        where T : INotifyPropertyChanged
    {
        private readonly Func<T, string[]?> _classDeterminator;
        private readonly Func<object, INotifyPropertyChanged?>[] _path;
        private readonly INotifyPropertyChanged?[] _pathItems;

        public RowListener(DataGridRow row, Func<object, INotifyPropertyChanged?>[] path, Func<T, string[]?> classDeterminator)
        {
            ArgumentNullException.ThrowIfNull(row);
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(classDeterminator);

            this.Row = row;
            this._path = path;
            this._pathItems = new INotifyPropertyChanged[path.Length];
            this._classDeterminator = classDeterminator;
        }

        public DataGridRow Row { get; }
        private T? Item { get; set; }
        private string[] LastClasses { get; set; } = Array.Empty<string>();

        public void Attach()
        {
            this.Row.DataContextChanged += this.DataContextChanged;
            this.DataContextChanged();
        }

        private void DataContextChanged(object? sender = null, EventArgs? e = default)
        {
            foreach (var pathItem in this._pathItems.NotNull())
            {
                pathItem.PropertyChanged -= this.RebuildPath;
            }

            this.RebuildPath();
        }

        private void RebuildPath(object? sender = null, PropertyChangedEventArgs? e = default)
        {
            var lastIndex = this._path.Length - 1;
            var current = this.Row;
            for (var i = 0; i < this._path.Length; i++)
            {
                var old = this._pathItems[i];
                var next = current is null ? null : this._path[i](current);
                this._pathItems[i] = next;
                
                if (i == lastIndex || object.ReferenceEquals(old, next))
                {
                    continue;
                }

                if (old is not null)
                {
                    old.PropertyChanged -= this.RebuildPath;
                }

                if (next is not null)
                {
                    next.PropertyChanged += this.RebuildPath;
                }
            }

            if (this.Item is not null)
            {
                this.Item.PropertyChanged -= this.Determine;
            }

            if (this._pathItems[lastIndex] is T a)
            {
                this.Item = a;
                this.Item.PropertyChanged += this.Determine;
                this.Determine();
            }
            else
            {
                this.Item = default;
            }
        }


        private void Determine(object? sender = null, PropertyChangedEventArgs? e = default)
        {
            if (this.Item is not { } a)
            {
                return;
            }

            var classes = this._classDeterminator(a) ?? Array.Empty<string>();
            foreach (var noMore in this.LastClasses.Except(classes))
            {
                this.Row.Classes.Remove(noMore);
            }

            this.LastClasses = classes;

            foreach (var now in classes)
            {
                this.Row.Classes.Add(now);
            }
        }

        public void Detach()
        {
            this.Row.DataContextChanged -= this.DataContextChanged;
            foreach (var pathItem in this._pathItems.NotNull())
            {
                pathItem.PropertyChanged -= this.RebuildPath;
            }

            if (this.Item is not null)
            {
                this.Item.PropertyChanged -= this.Determine;
            }

            foreach (var noMore in this.LastClasses)
            {
                this.Row.Classes.Remove(noMore);
            }
        }
    }

    #region AutoScroll Property

    /// <summary>
    /// Auto Scrolls the GridView to a newly added item, if no item is selected.
    /// </summary>
    public static readonly AttachedProperty<bool> AutoScrollProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, bool>(
            "AutoScroll",
            typeof(DataGrid));

    private static readonly AttachedProperty<NotifyCollectionChangedEventHandler?> AutoScrollHandlerProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, NotifyCollectionChangedEventHandler?>(
            "AutoScrollHandler",
            typeof(DataGrid));

    public static bool GetAutoScroll(DataGrid element) => element.GetValue(DataGridExtensions.AutoScrollProperty);

    public static void SetAutoScroll(DataGrid element, bool value) => element.SetValue(DataGridExtensions.AutoScrollProperty, value);

    private static void OnAutoScrollChanged(AvaloniaObject element, bool value)
    {
        if (element is not DataGrid grid)
        {
            return;
        }

        var verticalScrollBar = new Lazy<ScrollBar?>(() => grid.FindTemplatePart<ScrollBar>("PART_VerticalScrollbar"));
        NotifyCollectionChangedEventHandler? handler;
        if (value)
        {
            handler = AutoScroll;
            element.SetValue(DataGridExtensions.AutoScrollHandlerProperty, handler);
        }
        else
        {
            handler = element.GetValue(DataGridExtensions.AutoScrollHandlerProperty);
            element.ClearValue(DataGridExtensions.AutoScrollHandlerProperty);
        }

        if (handler is null || grid.ItemsSource is not INotifyCollectionChanged collection)
        {
            return;
        }

        if (value)
        {
            collection.CollectionChanged += handler;
        }
        else
        {
            collection.CollectionChanged -= handler;
        }

        return;

        void AutoScroll(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && grid.SelectedIndex == -1)
            {
                if (verticalScrollBar.Value is not null && verticalScrollBar.Value.IsExpanded)
                {
                    return;
                }

                grid.ScrollIntoView(e.NewItems?.Cast<object>().FirstOrDefault(), grid.CurrentColumn);
            }
        }
    }

    private static void OnItemsSourceChanged(AvaloniaObject element, IEnumerable? oldValue, IEnumerable? newValue)
    {
        if (element is not DataGrid grid || !DataGridExtensions.GetAutoScroll(grid))
        {
            return;
        }

        var handler = element.GetValue(DataGridExtensions.AutoScrollHandlerProperty);
        if (oldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= handler;
        }

        if (newValue is INotifyCollectionChanged newCollection)
        {
            newCollection.CollectionChanged += handler;
        }
    }

    #endregion
}
