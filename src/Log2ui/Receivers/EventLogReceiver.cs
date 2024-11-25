using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using Log2ui.Data;

namespace Log2ui.Receivers;

[Serializable]
[DisplayName("Windows Event Log")]
[SupportedOSPlatform("windows")]
public class EventLogReceiver : BaseReceiver
{
    private bool _appendHostNameToLogger = true;

    [NonSerialized]
    private string? _baseLoggerName;

    [NonSerialized]
    private EventLog? _eventLog;

    private string? _logName;
    private string _machineName = ".";
    private string? _source;


    [Category("Configuration")]
    [DisplayName("Event Log Name")]
    [Description("The name of the log on the specified computer.")]
    public string? LogName
    {
        get => this._logName;
        set => this._logName = value;
    }

    [Category("Configuration")]
    [DisplayName("Machine Name")]
    [Description("The computer on which the log exists.")]
    public string MachineName
    {
        get => this._machineName;
        set => this._machineName = value;
    }

    [Category("Configuration")]
    [DisplayName("Event Log Source")]
    [Description("The source of event log entries.")]
    public string? Source
    {
        get => this._source;
        set => this._source = value;
    }

    [Category("Behavior")]
    [DisplayName("Append Machine Name to Logger")]
    [Description("Append the remote Machine Name to the Logger Name.")]
    public bool AppendHostNameToLogger
    {
        get => this._appendHostNameToLogger;
        set => this._appendHostNameToLogger = value;
    }


    private void EventLogOnEntryWritten(object sender, EntryWrittenEventArgs entryWrittenEventArgs)
    {
        var logMsg = new LogMessage
        {
            RootLoggerName = this._baseLoggerName,
            LoggerName = string.IsNullOrEmpty(entryWrittenEventArgs.Entry.Source)
                             ? this._baseLoggerName
                             : $"{this._baseLoggerName}.{entryWrittenEventArgs.Entry.Source}",
            Message = entryWrittenEventArgs.Entry.Message,
            TimeStamp = entryWrittenEventArgs.Entry.TimeGenerated,
            Level = LogLevels.Of(EventLogReceiver.GetLogLevel(entryWrittenEventArgs.Entry.EntryType)),
            ThreadName = entryWrittenEventArgs.Entry.InstanceId.ToString()
        };

        if (!string.IsNullOrEmpty(entryWrittenEventArgs.Entry.Category))
        {
            logMsg.Properties.Add("Category", entryWrittenEventArgs.Entry.Category);
        }

        if (!string.IsNullOrEmpty(entryWrittenEventArgs.Entry.UserName))
        {
            logMsg.Properties.Add("User Name", entryWrittenEventArgs.Entry.UserName);
        }

        this.Notifiable.Notify(logMsg);
    }

    private static LogLevel GetLogLevel(EventLogEntryType entryType)
    {
        switch (entryType)
        {
            case EventLogEntryType.Warning:
                return LogLevel.Warn;
            case EventLogEntryType.FailureAudit:
            case EventLogEntryType.Error:
                return LogLevel.Error;
            case EventLogEntryType.SuccessAudit:
            case EventLogEntryType.Information:
                return LogLevel.Info;
            default:
                return LogLevel.Invalid;
        }
    }


    #region Overrides of BaseReceiver

    [Browsable(false)]
    public override string SampleClientConfig => """
                                                 Use Log2Console to display the Windows Event Logs.
                                                 Note that the Thread column is used to display the Instance ID (Event ID).
                                                 """;

    public override void Initialize()
    {
        if (string.IsNullOrEmpty(this.MachineName))
        {
            this.MachineName = ".";
        }

        this._eventLog = new EventLog(this.LogName, this.MachineName, this.Source);
        this._eventLog.EntryWritten += this.EventLogOnEntryWritten;
        this._eventLog.EnableRaisingEvents = true;

        this._baseLoggerName = this.AppendHostNameToLogger && !string.IsNullOrEmpty(this.MachineName) && this.MachineName != "."
                                   ? $"[Host: {this.MachineName}].{this.LogName}"
                                   : this.LogName;
    }

    public override void Terminate()
    {
        if (this._eventLog != null)
        {
            this._eventLog.Dispose();
        }

        this._eventLog = null;
    }

    #endregion
}
