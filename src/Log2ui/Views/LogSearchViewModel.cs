using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DynamicData.Binding;
using Log2ui.Collections;
using Log2ui.Data;
using Log2ui.Helpers;
using ReactiveUI;

namespace Log2ui.Views;

public class LogSearchViewModel : ViewModel
{
    private readonly ICollectionView<LogMessageItem> _collectionView;
    private Func<LogMessageItem, bool>? _currentFilter;
    private int _currentFoundIndex;
    private Func<LogMessageItem, bool>? _currentSearch;
    private string? _filterText;
    private ObservableCollection<LogSearchResult>? _found;
    private int? _foundCount;
    private string? _foundText;
    private LogSearchMode _mode;
    private string? _searchText;

    public LogSearchViewModel(ICollectionView<LogMessageItem> collectionView)
    {
        ArgumentNullException.ThrowIfNull(collectionView);

        this._collectionView = collectionView;
        this._mode = this.Modes.First();
        this._collectionView.CollectionChanged += this.CollectionView_OnCollectionChanged;

        this.SearchCommand = ReactiveCommand.CreateFromTask(this.SearchAsync, this.WhenAny(a => a.SearchText, a => !string.IsNullOrEmpty(a.Value)));
        this.FilterCommand = ReactiveCommand.Create(
            this.Filter,
            this.WhenAny(a => a.SearchText, a => a.CurrentFilter, (a, b) => !string.IsNullOrEmpty(a.Value) || b.Value is not null));
        this.PreviousCommand = ReactiveCommand.Create(
            this.Previous,
            this.WhenAny(a => a.FoundCount, a => a.CurrentFoundIndex, (a, _) => a.GetValueOrDefault() > 0));
        this.NextCommand = ReactiveCommand.Create(this.Next, this.WhenAny(a => a.FoundCount, a => a.CurrentFoundIndex, (a, _) => a.GetValueOrDefault() > 0));

        this.WhenValueChanged(a => a.SearchText).Subscribe(_ => this.Found = null);
        this.WhenAny(a => a.Found, a => a.FoundCount, (a, b) => a.Value is null ? null : $"Found {b.GetValueOrDefault()} results")
            .Subscribe(a => this.FoundText = a);
    }

    public ReactiveCommand<Unit, Unit> SearchCommand { get; }
    public ReactiveCommand<Unit, Unit> FilterCommand { get; set; }
    public ReactiveCommand<Unit, Unit> PreviousCommand { get; }
    public ReactiveCommand<Unit, Unit> NextCommand { get; }

    public LogSearchMode[] Modes { get; } =
    [
        new(
            "Any wildcard",
            "fa-solid fa-asterisk",
            "Case insensitive wildcards, Anywhere",
            searchText =>
            {
                searchText = $"*{searchText}*";
                return input => input.IsGlob(searchText);
            }),
        new(
            "Exact wildcard",
            "fa-solid fa-star-of-life",
            "Case sensitive wildcards, Beginning to end",
            searchText => input => input.IsGlob(searchText, true)),
        new(
            "Easy regex",
            "fa-solid fa-star-half-stroke",
            "Case sensitive wildcards",
            searchText =>
            {
                var regex = new Regex(searchText, RegexOptions.Compiled | RegexOptions.NonBacktracking | RegexOptions.IgnoreCase);
                return input => regex.IsMatch(input);
            }),
        new(
            "Regex",
            "fa-solid fa-star",
            "Case sensitive wildcards",
            searchText =>
            {
                var regex = new Regex(searchText, RegexOptions.Compiled);
                return input => regex.IsMatch(input);
            })
    ];

    public LogSearchMode Mode
    {
        get => this._mode;
        set => this.RaiseAndSetIfChanged(ref this._mode, value);
    }

    public string? SearchText
    {
        get => this._searchText;
        set => this.RaiseAndSetIfChanged(ref this._searchText, value);
    }

    public ObservableCollection<LogSearchResult>? Found
    {
        get => this._found;
        set
        {
            if (this._found is { } a)
            {
                a.CollectionChanged -= this.FoundCountChanged;
                foreach (var result in a)
                {
                    result.Item.Highlight = false;
                }
            }

            this.RaiseAndSetIfChanged(ref this._found, value);

            if (this._found is { } b)
            {
                b.CollectionChanged += this.FoundCountChanged;
            }

            this.FoundCountChanged();
            this.CurrentFoundIndex = -1;
        }
    }

    public int? FoundCount
    {
        get => this._foundCount;
        set => this.RaiseAndSetIfChanged(ref this._foundCount, value);
    }

    public string? FoundText
    {
        get => this._foundText;
        set => this.RaiseAndSetIfChanged(ref this._foundText, value);
    }

    public string? FilterText
    {
        get => this._filterText;
        set => this.RaiseAndSetIfChanged(ref this._filterText, value);
    }

    public Func<LogMessageItem, bool>? CurrentFilter
    {
        get => this._currentFilter;
        private set => this.RaiseAndSetIfChanged(ref this._currentFilter, value);
    }

    public Func<LogMessageItem, bool>? CurrentSearch
    {
        get => this._currentSearch;
        private set => this.RaiseAndSetIfChanged(ref this._currentSearch, value);
    }

    public int CurrentFoundIndex
    {
        get => this._currentFoundIndex;
        set => this.RaiseAndSetIfChanged(ref this._currentFoundIndex, value);
    }

    private void FoundCountChanged(object? sender = null, EventArgs? e = null)
    {
        this.FoundCount = this._found?.Count;
    }

    public void Previous()
    {
        if (this.Found is null)
        {
            return;
        }

        var jumpTo = this.CurrentFoundIndex;
        jumpTo = --jumpTo <= 0 ? (this.FoundCount ?? 0) - 1 : jumpTo;
        this.CurrentFoundIndex = jumpTo;
        var item = this.Found[jumpTo];
        this._collectionView.MoveCurrentTo(item.Item);
    }

    public void Next()
    {
        if (this.Found is null)
        {
            return;
        }

        var jumpTo = this.CurrentFoundIndex;
        jumpTo = ++jumpTo >= this.FoundCount ? 0 : jumpTo;
        this.CurrentFoundIndex = jumpTo;
        var item = this.Found[jumpTo];
        this._collectionView.MoveCurrentTo(item.Item);
    }

    private async Task SearchAsync()
    {
        if (string.IsNullOrEmpty(this.SearchText))
        {
            return;
        }

        var searchFunc = this.CurrentSearch = this.BuildSearchFunc(this.SearchText);
        var (found, unmarks) = await Task.Run(
                                   () =>
                                   {
                                       var results = this._collectionView
                                                         .Chunk(1000).AsParallel()
                                                         .SelectMany(c => c.Where(searchFunc))
                                                         .Select(a => new LogSearchResult(a))
                                                         .AsSequential().ToList();
                                       var nolonger = (this.Found?.AsEnumerable() ?? Array.Empty<LogSearchResult>())
                                                      .ExceptBy(results.Select(a => a.Item), a => a.Item)
                                                      .ToList();
                                       return (new ObservableCollection<LogSearchResult>(results), nolonger);
                                   });
        this.Found = found;
        foreach (var unmark in unmarks)
        {
            unmark.Item.Highlight = false;
        }

        foreach (var result in found)
        {
            result.Item.Highlight = true;
        }
    }

    private void CollectionView_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (this.CurrentSearch is null || this.Found is null || e.Action != NotifyCollectionChangedAction.Add)
        {
            return;
        }

        foreach (var item in e.NewItems?.OfType<LogMessageItem>().Where(this.CurrentSearch) ?? Array.Empty<LogMessageItem>())
        {
            this.Found.Add(new LogSearchResult(item));
            item.Highlight = true;
        }
    }

    private Func<LogMessageItem, bool> BuildSearchFunc(string searchText)
    {
        var searchFuncProto = this.Mode.Build(searchText);
        return a => MessageSearchFunc(a.Message);

        bool SearchFunc(string? a) => !string.IsNullOrEmpty(a) && searchFuncProto(a);

        bool MessageSearchFunc(LogMessage a) => SearchFunc(a.Message)
                                                || SearchFunc(a.LoggerName)
                                                || SearchFunc(a.ThreadName)
                                                || SearchFunc(a.ExceptionString)
                                                || SearchFunc(a.TimeStampString);
    }

    private void Filter()
    {
        if (this.CurrentFilter is not null)
        {
            this.CurrentFilter = null;
            this.FilterText = null;
            return;
        }

        if (string.IsNullOrEmpty(this.SearchText))
        {
            return;
        }

        var searchFunc = this.BuildSearchFunc(this.SearchText);
        this.FilterText = $"Filtering by '{this.SearchText}'";
        this.CurrentFilter = searchFunc;
    }

    public readonly struct LogSearchResult(LogMessageItem item)
    {
        public LogMessageItem Item { get; } = item;
    }
}
