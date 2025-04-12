using System.Runtime.CompilerServices;
using Avalonia.Logging;

namespace Log2ui.Tools;

public class AvaloniaToSerilogSink : ILogSink
{
    public bool IsEnabled(LogEventLevel level, string area)
    {
        return true;
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        this.Log(level, area, source, messageTemplate, []);
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        var logger = Serilog.Log.ForContext("SourceContext", area);

        var loglevel = level switch
        {
            LogEventLevel.Verbose => Serilog.Events.LogEventLevel.Verbose,
            LogEventLevel.Debug => Serilog.Events.LogEventLevel.Debug,
            LogEventLevel.Information => Serilog.Events.LogEventLevel.Information,
            LogEventLevel.Warning => Serilog.Events.LogEventLevel.Warning,
            LogEventLevel.Error => Serilog.Events.LogEventLevel.Error,
            LogEventLevel.Fatal => Serilog.Events.LogEventLevel.Fatal,
            _ => throw new SwitchExpressionException(level),
        };
        logger.Write(loglevel, messageTemplate, propertyValues);
    }
}
