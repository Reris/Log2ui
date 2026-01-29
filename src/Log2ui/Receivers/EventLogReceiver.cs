using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using Log2ui.Collections;
using Log2ui.Data;
using Log2ui.Dependencies;
using Log2ui.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Receivers;

[SupportedOSPlatform("windows")]
public class EventLogReceiver(EventLogReceiver.Settings settings) : BaseReceiver, ISelfRegistering
{
    [NonSerialized]
    private string? _baseLoggerName;

    [NonSerialized]
    private EventLog? _eventLog;

    public static void RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<EventLogReceiver>();
        ReceiverSettingsDiscriminatorAttribute.Register<Settings>(registry.Collection);
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
            Level = EventLogReceiver.GetLogLevel(entryWrittenEventArgs.Entry.EntryType),
            ThreadName = entryWrittenEventArgs.Entry.InstanceId.ToString(),
        };

        if (!string.IsNullOrEmpty(entryWrittenEventArgs.Entry.Category))
        {
            logMsg.Properties.Add("Category", entryWrittenEventArgs.Entry.Category);
        }

        if (!string.IsNullOrEmpty(entryWrittenEventArgs.Entry.UserName))
        {
            logMsg.Properties.Add("User Name", entryWrittenEventArgs.Entry.UserName);
        }

        this.Notify(logMsg);
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

    [ReceiverSettingsDiscriminator(nameof(EventLogReceiver), 1)]
    public record Settings() : ReceiverSettings(Settings.DefaultProperties)
    {
        private static readonly EquatableArray<LogColumn> DefaultProperties = [];

        public override string Key => ReceiverSettings.CreateKey<EventLogReceiver>(this.LogName, this.MachineName, this.Source);
        public override string DisplayName => $"Event Log {this.Source}";
        public override string TypeDisplayName => "Windows Event Log";


        [Category("Configuration")]
        [DisplayName("Event Log Name")]
        [Description("The name of the log on the specified computer.")]
        public string? LogName
        {
            get;
            set => this.SetField(ref field, value);
        }

        [Category("Configuration")]
        [DisplayName("Machine Name")]
        [Description("The computer on which the log exists.")]
        [DefaultValue(".")]
        public string MachineName
        {
            get;
            set => this.SetField(ref field, value);
        } = ".";

        [Category("Configuration")]
        [DisplayName("Event Log Source")]
        [Description("The source of event log entries.")]
        public string? Source
        {
            get;
            set => this.SetField(ref field, value);
        }

        [Category("Behavior")]
        [DisplayName("Append Machine Name to Logger")]
        [Description("Append the remote Machine Name to the Logger Name.")]
        [DefaultValue(true)]
        public bool AppendHostNameToLogger
        {
            get;
            set => this.SetField(ref field, value);
        } = true;

        public override ReceiverSettings DeepClone()
        {
            return this with { };
        }

        public override IReceiver CreateReceiver(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<EventLogReceiver>(serviceProvider, this);
        }
    }


    #region Overrides of BaseReceiver

    [Browsable(false)]
    public override string SampleClientConfig => """
                                                 Use Log2ui to display the Windows Event Logs.
                                                 Note that the Thread column is used to display the Instance ID (Event ID).
                                                 """;

    public override bool IsAlive => this._eventLog is not null;

    protected override void Initialize()
    {
        this._eventLog = new EventLog(settings.LogName, settings.MachineName, settings.Source);
        this._eventLog.EntryWritten += this.EventLogOnEntryWritten;
        this._eventLog.EnableRaisingEvents = true;

        this._baseLoggerName = settings.AppendHostNameToLogger && !string.IsNullOrEmpty(settings.MachineName) && settings.MachineName != "."
                                   ? $"[Host: {settings.MachineName}].{settings.LogName}"
                                   : settings.LogName;
    }

    protected override void Terminate()
    {
        this._eventLog?.Dispose();
        this._eventLog = null;
    }

    #endregion
}
