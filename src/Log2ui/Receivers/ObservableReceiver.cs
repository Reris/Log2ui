using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Log2ui.Data;
using Serilog.Events;

namespace Log2ui.Receivers;

public class ObservableReceiver(IObservable<LogEvent> observable) : BaseReceiver
{
    private IDisposable? _subscription;

    public override string SampleClientConfig => string.Empty;

    public override void Initialize()
    {
        this._subscription = observable.Subscribe(a => this.Notify(this.Convert(a)));
    }

    private LogMessage Convert(LogEvent logEvent)
    {
        return new LogMessage
        {
            Level = logEvent.Level switch
            {
                LogEventLevel.Verbose => LogLevels.Of(LogLevel.Trace),
                LogEventLevel.Debug => LogLevels.Of(LogLevel.Debug),
                LogEventLevel.Information => LogLevels.Of(LogLevel.Info),
                LogEventLevel.Warning => LogLevels.Of(LogLevel.Warn),
                LogEventLevel.Error => LogLevels.Of(LogLevel.Error),
                LogEventLevel.Fatal => LogLevels.Of(LogLevel.Fatal),
                _ => throw new SwitchExpressionException(logEvent.Level),
            },
            CallSiteClass = ObservableReceiver.FindProperty(logEvent, "Class"),
            CallSiteMethod = ObservableReceiver.FindProperty(logEvent, "Method"),
            ExceptionString = logEvent.Exception?.ToString(),
            LoggerName = ObservableReceiver.FindProperty(logEvent, "SourceContext"),
            Message = logEvent.MessageTemplate.ToString(),
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

    public override void Terminate()
    {
        this._subscription?.Dispose();
    }
}
