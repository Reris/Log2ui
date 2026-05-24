using System;
using CsvHelper.Configuration.Attributes;
using Log2ui.Collections;

namespace Log2ui.Data;

public record LogMessage
{
    /// <summary>
    /// Properties collection.
    /// </summary>
    [Name("properties")]
    public EquatableDictionary<string, string> Properties { get; set; }

    /// <summary>
    /// Log Message.
    /// </summary>
    [Name("message")]
    public string? Message { get; set; }

    /// <summary>
    /// The CallSite Class
    /// </summary>
    [Name("class")]
    public string? CallSiteClass { get; set; }

    /// <summary>
    /// The CallSite Method in which the Log is made
    /// </summary>
    [Name("method")]
    public string? CallSiteMethod { get; set; }

    /// <summary>
    /// An exception message to associate to this message.
    /// </summary>
    [Name("exception")]
    public string? ExceptionString { get; set; }

    /// <summary>
    /// Log Level.
    /// </summary>
    [Name("level")]
    public LogLevel Level { get; set; } = LogLevel.Invalid;

    /// <summary>
    /// Logger Name.
    /// </summary>
    [Name("logger")]
    public string? LoggerName { get; set; }

    /// <summary>
    /// Root Logger Name.
    /// </summary>
    [Name("root")]
    public string? RootLoggerName { get; set; }

    /// <summary>
    /// The Line Number of the Log Message
    /// </summary>
    [Name("sequence")]
    public ulong SequenceNr { get; set; }

    /// <summary>
    /// The Line of the Source File
    /// </summary>
    [Name("fileline")]
    public uint SourceFileLineNr { get; set; }

    /// <summary>
    /// The Name of the Source File
    /// </summary>
    [Name("file")]
    public string? SourceFileName { get; set; }

    /// <summary>
    /// Thread Name.
    /// </summary>
    [Name("thread")]
    public string? ThreadName { get; set; }

    /// <summary>
    /// Time Stamp.
    /// </summary>
    [Name("time")]
    public DateTime TimeStamp
    {
        get;
        set
        {
            field = value;
            this.TimeStampString = field.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
    }

    /// <summary>
    /// Time Stamp as formatted string.
    /// </summary>
    [Ignore]
    public string TimeStampString { get; private set; } = string.Empty;

    public void CheckNull()
    {
        if (string.IsNullOrEmpty(this.LoggerName))
        {
            this.LoggerName = "Unknown";
        }

        if (string.IsNullOrEmpty(this.RootLoggerName))
        {
            this.RootLoggerName = "Unknown";
        }

        if (string.IsNullOrEmpty(this.Message))
        {
            this.Message = "Unknown";
        }

        if (string.IsNullOrEmpty(this.ThreadName))
        {
            this.ThreadName = string.Empty;
        }

        if (string.IsNullOrEmpty(this.ExceptionString))
        {
            this.ExceptionString = string.Empty;
        }

        if (string.IsNullOrEmpty(this.ExceptionString))
        {
            this.ExceptionString = string.Empty;
        }

        if (string.IsNullOrEmpty(this.CallSiteClass))
        {
            this.CallSiteClass = string.Empty;
        }

        if (string.IsNullOrEmpty(this.CallSiteMethod))
        {
            this.CallSiteMethod = string.Empty;
        }

        if (string.IsNullOrEmpty(this.SourceFileName))
        {
            this.SourceFileName = string.Empty;
        }

        if (this.Level == LogLevel.Invalid)
        {
            this.Level = LogLevel.Error;
        }
    }
}
