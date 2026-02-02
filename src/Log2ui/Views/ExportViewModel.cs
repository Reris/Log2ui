using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Log2ui.Data;
using Log2ui.Dependencies;
using Log2ui.Exporters;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace Log2ui.Views;

public class ExportViewModel : ViewModel, ISelfRegistering
{
    private readonly Subject<Unit> _exported;
    private readonly IReadOnlyCollection<LogMessageItem> _logMessageItems;
    private readonly IServiceProvider _serviceProvider;
    private CancellationTokenSource? _cts;

    public ExportViewModel(IReadOnlyCollection<LogMessageItem> logMessageItems, ExportSettings settings, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(logMessageItems);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        this._logMessageItems = logMessageItems;
        this._serviceProvider = serviceProvider;
        this.Settings = settings;

        this._exported = new Subject<Unit>().DisposeWith(this.Disposables);

        this.Header = $"Export {settings.DisplayName}";
        this.ExportCommand = ReactiveCommand.CreateFromTask(this.ExportAsync);
        this.CancelCommand = ReactiveCommand.Create(this.Cancel);
    }

    public ExportSettings Settings { get; }
    public ReactiveCommand<Unit, Unit> ExportCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public IObservable<Unit> Exported => this._exported.AsObservable();
    public string Header { get; }

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<ExportViewModel>();
    }

    public async Task ExportAsync()
    {
        using var cts = this._cts = new CancellationTokenSource();
        var exporter = this.Settings.CreateExporter(this._serviceProvider);
        await exporter.ExportAsync(this._logMessageItems.Select(a => a.Message), cts.Token);
        this._exported.OnNext(Unit.Default);
    }

    public void Cancel()
    {
        this._cts?.Cancel();
        this._cts?.Dispose();
        this._exported.OnNext(Unit.Default);
    }
}
