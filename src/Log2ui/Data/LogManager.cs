using System;
using System.Collections.Generic;

namespace Log2ui.Data;

public class LogManager : ILogManager
{
    public LogManager(LoggerItem rootLoggerItem)
    {
        this.RootLoggerItem = rootLoggerItem;
        this.FullPathLoggers = new Dictionary<string, LoggerItem>();
    }

    private LoggerItem RootLoggerItem { get; }
    public Dictionary<string, LoggerItem> FullPathLoggers { get; }

    public void ClearAll()
    {
        this.ClearLogMessages();

        this.RootLoggerItem.ClearAll();
        this.FullPathLoggers.Clear();
    }

    public void ClearLogMessages() => this.RootLoggerItem.ClearAllLogMessages();
    public void DeactivateLogger() => this.RootLoggerItem.Enabled = false;

    public LogMessageItem ProcessLogMessage(LogMessage logMessage)
    {
        // Check 1st in the global LoggerPath/Logger dictionary
        logMessage.CheckNull();

        if (!this.FullPathLoggers.TryGetValue(logMessage.LoggerName, out var logger))
        {
            // Not found, create one
            logger = this.RootLoggerItem.GetOrCreateLogger(logMessage.LoggerName);
        }

        if (logger == null)
        {
            throw new Exception("No Logger for this Log Message.");
        }

        return logger.AddLogMessage(logMessage);
    }

    public IEnumerable<LogMessageItem> ProcessLogMessage(IEnumerable<LogMessage> logMessages)
    {
        foreach (var logMessage in logMessages)
        {
            yield return this.ProcessLogMessage(logMessage);
        }
    }


    public void SearchText(string str) => this.RootLoggerItem.SearchText(str);

    public void SetRootLoggerName(string name) => this.RootLoggerItem.Name = name;
}
