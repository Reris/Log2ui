using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using Log2ui.Data;

namespace Log2ui.Receivers;

/// <summary>
/// This receiver watch a given file, like a 'tail' program, with one log event by line.
/// Ideally the log events should use the log4j XML Schema layout.
/// </summary>
[Serializable]
[DisplayName("Log File (Flat or Log4j XML Formatted)")]
public class FileReceiver : BaseReceiver
{
    public enum FileFormatEnums
    {
        Log4jXml,
        Flat
    }

    private FileFormatEnums _fileFormat;

    [NonSerialized]
    private string? _filename;

    [NonSerialized]
    private StreamReader? _fileReader;

    private string _fileToWatch = string.Empty;


    [NonSerialized]
    private FileSystemWatcher? _fileWatcher;

    [NonSerialized]
    private string? _fullLoggerName;

    [NonSerialized]
    private long _lastFileLength;

    private string? _loggerName;
    private bool _showFromBeginning;


    [Category("Configuration")]
    [DisplayName("File to Watch")]
    public string FileToWatch
    {
        get => this._fileToWatch;
        set
        {
            if (string.Equals(this._fileToWatch, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            this._fileToWatch = value;

            this.Restart();
        }
    }

    [Category("Configuration")]
    [DisplayName("File Format (Flat or Log4j XML)")]
    public FileFormatEnums FileFormat
    {
        get => this._fileFormat;
        set => this._fileFormat = value;
    }

    [Category("Configuration")]
    [DisplayName("Show from Beginning")]
    [Description("Show file contents from the beginning (not just newly appended lines)")]
    [DefaultValue(false)]
    public bool ShowFromBeginning
    {
        get => this._showFromBeginning;
        set
        {
            this._showFromBeginning = value;

            if (value && this._lastFileLength == 0)
            {
                this.ReadFile();
            }
        }
    }

    [Category("Behavior")]
    [DisplayName("Logger Name")]
    [Description("Append the given Name to the Logger Name. If left empty, the filename will be used.")]
    public string? LoggerName
    {
        get => this._loggerName;
        set
        {
            this._loggerName = value;

            this.ComputeFullLoggerName();
        }
    }


    private void Restart()
    {
        this.Terminate();
        this.Initialize();
    }

    private void ComputeFullLoggerName()
    {
        this._fullLoggerName = $"FileLogger.{(string.IsNullOrEmpty(this._loggerName)
                                                  ? this._filename.Replace('.', '_')
                                                  : this._loggerName)}";

        this.DisplayName = string.IsNullOrEmpty(this._loggerName)
                               ? string.Empty
                               : $"Log File [{this._loggerName}]";
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Changed)
        {
            return;
        }

        this.ReadFile();
    }

    private void ReadFile()
    {
        if (this._fileReader == null || this._fileReader.BaseStream.Length == this._lastFileLength)
        {
            return;
        }

        // Seek to the last file length
        this._fileReader.BaseStream.Seek(this._lastFileLength, SeekOrigin.Begin);

        // Get last added lines
        var sb = new StringBuilder();
        var logMsgs = new List<LogMessage>();

        while (this._fileReader.ReadLine() is { } line)
        {
            if (this._fileFormat == FileFormatEnums.Flat)
            {
                var logMsg = new LogMessage
                {
                    RootLoggerName = this._loggerName,
                    LoggerName = this._fullLoggerName,
                    ThreadName = "NA",
                    Message = line,
                    TimeStamp = DateTime.Now,
                    Level = LogLevel.Info,
                };

                logMsgs.Add(logMsg);
            }
            else
            {
                sb.Append(line);

                // This condition allows us to process events that spread over multiple lines
                if (line.Contains("</log4j:event>"))
                {
                    var logMsg = ReceiverUtils.ParseLog4JXmlLogEvent(sb.ToString(), this._fullLoggerName);
                    logMsgs.Add(logMsg);
                    sb = new StringBuilder();
                }
            }
        }

        // Notify the UI with the set of messages
        this.Notify(logMsgs);

        // Update the last file length
        this._lastFileLength = this._fileReader.BaseStream.Position;
    }


    #region IReceiver Members

    public override string SampleClientConfig => "Configuration for log4net:" + Environment.NewLine +
                                                 "<appender name=\"FileAppender\" type=\"log4net.Appender.FileAppender\">" + Environment.NewLine +
                                                 "    <file value=\"log-file.txt\" />" + Environment.NewLine +
                                                 "    <appendToFile value=\"true\" />" + Environment.NewLine +
                                                 "    <lockingModel type=\"log4net.Appender.FileAppender+MinimalLock\" />" + Environment.NewLine +
                                                 "    <layout type=\"log4net.Layout.XmlLayoutSchemaLog4j\" />" + Environment.NewLine +
                                                 "</appender>";

    public override void Initialize()
    {
        if (string.IsNullOrEmpty(this._fileToWatch))
        {
            return;
        }

        this._fileReader = new StreamReader(new FileStream(this._fileToWatch, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

        this._lastFileLength = this._showFromBeginning ? 0 : this._fileReader.BaseStream.Length;

        var path = Path.GetDirectoryName(this._fileToWatch);
        this._filename = Path.GetFileName(this._fileToWatch);
        this._fileWatcher = new FileSystemWatcher(path, this._filename);
        this._fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
        this._fileWatcher.Changed += this.OnFileChanged;
        this._fileWatcher.EnableRaisingEvents = true;

        this.ComputeFullLoggerName();
    }

    public override void Terminate()
    {
        if (this._fileWatcher != null)
        {
            this._fileWatcher.EnableRaisingEvents = false;
            this._fileWatcher.Changed -= this.OnFileChanged;
            this._fileWatcher = null;
        }

        this._fileReader?.Close();
        this._fileReader = null;
        this._lastFileLength = 0;
    }

    protected override void OnAttached(ILogMessageNotifiable notifiable)
    {
        base.OnAttached(notifiable);

        if (this._showFromBeginning)
        {
            this.ReadFile();
        }
    }

    #endregion
}
