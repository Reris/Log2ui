using System;
using System.Collections.Generic;
using CsvHelper.Configuration.Attributes;

namespace Log2ui.Data;

public class LogMessage
{
    private DateTime _timeStamp;

    /// <summary>
    /// Properties collection.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new();

    /// <summary>
    /// Log Message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// The CallSite Class
    /// </summary>
    public string? CallSiteClass { get; set; }

    /// <summary>
    /// The CallSite Method in which the Log is made
    /// </summary>
    public string? CallSiteMethod { get; set; }

    /// <summary>
    /// An exception message to associate to this message.
    /// </summary>
    public string? ExceptionString { get; set; }

    /// <summary>
    /// Log Level.
    /// </summary>
    public LogLevel Level { get; set; } = LogLevel.Invalid;

    /// <summary>
    /// Logger Name.
    /// </summary>
    public string? LoggerName { get; set; }

    /// <summary>
    /// Root Logger Name.
    /// </summary>
    public string? RootLoggerName { get; set; }

    /// <summary>
    /// The Line Number of the Log Message
    /// </summary>
    public ulong SequenceNr { get; set; }

    /// <summary>
    /// The Line of the Source File
    /// </summary>
    public uint SourceFileLineNr { get; set; }

    /// <summary>
    /// The Name of the Source File
    /// </summary>
    public string? SourceFileName { get; set; }

    /// <summary>
    /// Thread Name.
    /// </summary>
    public string? ThreadName { get; set; }

    /// <summary>
    /// Time Stamp.
    /// </summary>
    public DateTime TimeStamp
    {
        get => this._timeStamp;
        set
        {
            this._timeStamp = value;
            this.TimeStampString = this._timeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
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
