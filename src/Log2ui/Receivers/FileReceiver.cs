using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using Log2ui.Collections;
using Log2ui.Data;
using Log2ui.Dependencies;
using Log2ui.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Receivers;

/// <summary>
/// This receiver watch a given file, like a 'tail' program, with one log event by line.
/// Ideally the log events should use the log4j XML Schema layout.
/// </summary>
public class FileReceiver(FileReceiver.Settings settings) : BaseReceiver, ISelfRegistering
{
    public enum FileFormatEnums
    {
        Log4JXml,
        Flat,
    }

    [NonSerialized]
    private string? _filename;

    [NonSerialized]
    private StreamReader? _fileReader;

    [NonSerialized]
    private FileSystemWatcher? _fileWatcher;

    [NonSerialized]
    private string? _fullLoggerName;

    [NonSerialized]
    private long _lastFileLength;

    public static void RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<FileReceiver>();
        ReceiverSettingsDiscriminatorAttribute.Register<Settings>(registry.Collection);
    }

    private void Restart()
    {
        this.Terminate();
        this.Initialize();
    }

    private void ComputeFullLoggerName()
    {
        this._fullLoggerName = $"FileLogger.{(string.IsNullOrEmpty(settings.LoggerName)
                                                  ? this._filename.Replace('.', '_')
                                                  : settings.LoggerName)}";
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
            if (settings.FileFormat == FileFormatEnums.Flat)
            {
                var logMsg = new LogMessage
                {
                    RootLoggerName = settings.LoggerName,
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

    [ReceiverSettingsDiscriminator(nameof(FileReceiver), 1)]
    public record Settings() : ReceiverSettings(Settings.DefaultMappings)
    {
        private static readonly EquatableArray<FieldMapping> DefaultMappings = [];

        public override string Key => ReceiverSettings.CreateKey("File", this.GetLoggerName());
        public override string DisplayName => $"Log File {this.GetLoggerName()}";
        public override string TypeDisplayName => "Log File";

        [Category("Configuration")]
        [DisplayName("File to Watch")]
        [DefaultValue("")]
        public string FileToWatch
        {
            get;
            set => this.SetField(ref field, value);
        } = "";

        [Category("Configuration")]
        [DisplayName("File Format (Flat or Log4j XML)")]
        public FileFormatEnums FileFormat
        {
            get;
            set => this.SetField(ref field, value);
        }

        [Category("Configuration")]
        [DisplayName("Show from Beginning")]
        [Description("Show file contents from the beginning (not just newly appended lines)")]
        [DefaultValue(false)]
        public bool ShowFromBeginning
        {
            get;
            set => this.SetField(ref field, value);
        }

        [Category("Behavior")]
        [DisplayName("Logger Name")]
        [Description("Append the given Name to the Logger Name. If left empty, the filename will be used.")]
        public string? LoggerName
        {
            get;
            set => this.SetField(ref field, value);
        }

        public override ReceiverSettings DeepClone()
        {
            return this with { };
        }

        public override IReceiver CreateReceiver(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<FileReceiver>(serviceProvider, this);
        }

        public string GetLoggerName()
        {
            return (!string.IsNullOrWhiteSpace(this.LoggerName) ? this.LoggerName : Path.GetFileNameWithoutExtension(this.FileToWatch)) ?? "File";
        }
    }


    #region IReceiver Members

    public override string SampleClientConfig => "Configuration for log4net:" + Environment.NewLine +
                                                 "<appender name=\"FileAppender\" type=\"log4net.Appender.FileAppender\">" + Environment.NewLine +
                                                 "    <file value=\"log-file.txt\" />" + Environment.NewLine +
                                                 "    <appendToFile value=\"true\" />" + Environment.NewLine +
                                                 "    <lockingModel type=\"log4net.Appender.FileAppender+MinimalLock\" />" + Environment.NewLine +
                                                 "    <layout type=\"log4net.Layout.XmlLayoutSchemaLog4j\" />" + Environment.NewLine +
                                                 "</appender>";

    public override bool IsAlive => this._fileReader is not null && this._fileWatcher is not null;

    protected override void Initialize()
    {
        if (string.IsNullOrEmpty(settings.FileToWatch))
        {
            return;
        }

        this._fileReader = new StreamReader(new FileStream(settings.FileToWatch, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

        this._lastFileLength = settings.ShowFromBeginning ? 0 : this._fileReader.BaseStream.Length;

        var path = Path.GetDirectoryName(settings.FileToWatch);
        this._filename = Path.GetFileName(settings.FileToWatch);
        this._fileWatcher = new FileSystemWatcher(path, this._filename);
        this._fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
        this._fileWatcher.Changed += this.OnFileChanged;
        this._fileWatcher.EnableRaisingEvents = true;

        this.ComputeFullLoggerName();
    }

    protected override void Terminate()
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

        if (settings.ShowFromBeginning)
        {
            this.ReadFile();
        }
    }

    #endregion
}
