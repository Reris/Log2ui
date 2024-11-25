using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Log2ui.Helpers;
using ReactiveUI;

namespace Log2ui.Views;

public class MainWindowViewModel : ViewModel
{
    private readonly IMainDispatcher _mainDispatcher;
    private LoggerViewModel? _selected;

    public MainWindowViewModel(IMainDispatcher mainDispatcher, IViewModelFactory viewModelFactory)
    {
        this._mainDispatcher = mainDispatcher;
        this.CreateLogger = () =>
        {
            var name = this.CreateLoggerName();
            return viewModelFactory.Create<LoggerViewModel>(name);
        };

        this.Loggers.CollectionChanged += this.LoggersOnCollectionChanged;
        this.AddLogger();
    }

    public ObservableCollection<LoggerViewModel> Loggers { get; } = [];
    public Func<LoggerViewModel> CreateLogger { get; }

    public LoggerViewModel? Selected
    {
        get => this._selected;
        set => this.RaiseAndSetIfChanged(ref this._selected, value);
    }

    private void LoggersOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Remove:
            {
                var logger = e.OldItems!.Cast<LoggerViewModel>().Single();
                logger.Dispose();

                if (this.Loggers.Count == 0)
                {
                    this._mainDispatcher.InvokeAsync(
                        async () =>
                        {
                            await Task.Yield();
                            this.AddLogger();
                        });
                }

                break;
            }
        }
    }

    public string CreateLoggerName()
    {
        for (var i = 1; i < 10000; i++)
        {
            var name = $"Log{i}";
            if (this.Loggers.All(a => a.Name != name))
            {
                return name;
            }
        }

        throw new IndexOutOfRangeException("Too many loggers.");
    }

    public void AddLogger()
    {
        var logger = this.CreateLogger();
        this.Loggers.Add(logger);
        this.Selected = this.Loggers.Last();
    }
}
