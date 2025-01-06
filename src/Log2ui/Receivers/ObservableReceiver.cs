using System;
using System.Linq;
using Log2ui.Data;
using Log2ui.Dependencies;
using Log2ui.Settings;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;

namespace Log2ui.Receivers;

public class ObservableReceiver(ObservableReceiver.Settings settings, IObservable<LogEvent> observable) : BaseReceiver, ISelfRegistering
{
    private IDisposable? _subscription;

    public override string SampleClientConfig => string.Empty;

    public static void RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<ObservableReceiver>();
        ReceiverSettingsKeyAttribute.Register<Settings>(registry.Collection);
    }

    protected override void Initialize()
    {
        this._subscription = observable.Subscribe(a => this.Notify(this.Convert(a)));
    }

    private LogMessage Convert(LogEvent logEvent)
    {
        return new LogMessage
        {
            Level = logEvent.Level switch
            {
                LogEventLevel.Verbose => LogLevel.Trace,
                LogEventLevel.Debug => LogLevel.Debug,
                LogEventLevel.Information => LogLevel.Info,
                LogEventLevel.Warning => LogLevel.Warn,
                LogEventLevel.Error => LogLevel.Error,
                LogEventLevel.Fatal => LogLevel.Fatal,
                _ => LogLevel.Invalid,
            },
            CallSiteClass = ObservableReceiver.FindProperty(logEvent, "Class"),
            CallSiteMethod = ObservableReceiver.FindProperty(logEvent, "Method"),
            ExceptionString = logEvent.Exception?.ToString(),
            LoggerName = ObservableReceiver.FindProperty(logEvent, "SourceContext"),
            Message = logEvent.MessageTemplate.Render(logEvent.Properties),
            RootLoggerName = ObservableReceiver.FindProperty(logEvent, "RootLogger"),
            SequenceNr = ulong.TryParse(ObservableReceiver.FindProperty(logEvent, "SequenceNumber"), out var sqlNr) ? sqlNr : 0,
            SourceFileLineNr = uint.TryParse(ObservableReceiver.FindProperty(logEvent, "LineNumber"), out var lineNr) ? lineNr : 0,
            SourceFileName = ObservableReceiver.FindProperty(logEvent, "FileName"),
            ThreadName = ObservableReceiver.FindProperty(logEvent, "ThreadName"),
            TimeStamp = logEvent.Timestamp.DateTime,
            Properties = logEvent.Properties.ToDictionary(a => a.Key, a => a.Value.ToString()),
        };
    }

    private static string? FindProperty(LogEvent logEvent, string propertyName)
    {
        return logEvent.Properties.TryGetValue(propertyName, out var callSiteClass)
                   ? callSiteClass.ToString()
                   : null;
    }

    protected override void Terminate()
    {
        this._subscription?.Dispose();
    }

    [ReceiverSettingsKey(nameof(ObservableReceiver), 1)]
    public record Settings : ReceiverSettings
    {
        public override string ValueKey => nameof(ObservableReceiver);

        public override ReceiverSettings DeepClone()
        {
            return this with { };
        }

        public override IReceiver CreateReceiver(IServiceProvider serviceProvider)
        {
            var observable = serviceProvider.GetRequiredService<IObservable<LogEvent>>();
            return ActivatorUtilities.CreateInstance<ObservableReceiver>(serviceProvider, this, observable);
        }
    }
}
