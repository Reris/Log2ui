using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Log2ui.Collections;
using Log2ui.Data;
using Log2ui.Dependencies;
using Log2ui.Settings;
using Microsoft.Extensions.DependencyInjection;
using PropertyModels.ComponentModel.DataAnnotations;
using Splat;
using LogLevel = Log2ui.Data.LogLevel;

namespace Log2ui.Receivers;

/// <summary>
/// This receiver watch a given file, like a 'tail' program, with one log event by line.
/// Ideally the log events should use the log4j XML Schema layout.
/// </summary>
[DisplayName("CSV Log File")]
public class CsvFileReceiver : BaseReceiver, ISelfRegistering
{
    private readonly CsvConfiguration _csvConfigurationBeginFile;
    private readonly CsvConfiguration _csvConfigurationDuringFile;
    private readonly IFileSystem _fileSystem;
    private readonly Settings _settings;
    private StreamReader? _fileReader;
    private IFileSystemWatcher? _fileWatcher;
    private Task? _readFileStack;

    /// <summary>
    /// This receiver watch a given file, like a 'tail' program, with one log event by line.
    /// Ideally the log events should use the log4j XML Schema layout.
    /// </summary>
    public CsvFileReceiver(Settings settings, IFileSystem fileSystem)
    {
        this._settings = settings;
        this._fileSystem = fileSystem;

        this._csvConfigurationBeginFile = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = settings.HasHeader,
            Delimiter = settings.Delimiter,
            Quote = settings.QuoteChar.Length == 1 ? settings.QuoteChar[0] : '"',
        };
        this._csvConfigurationDuringFile = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            Delimiter = settings.Delimiter,
            Quote = settings.QuoteChar.Length == 1 ? settings.QuoteChar[0] : '"',
        };
    }

    [Browsable(false)]
    public override string SampleClientConfig => @"<target name=""CsvLog"" 
        xsi:type=""File"" 
        fileName=""${basedir}/Logs/log.csv""
        archiveFileName=""${basedir}/Logs/Archives/log_{#}_${date:format=yyyy-MM-d_HH}.txt""
        archiveEvery=""Hour""
        archiveNumbering=""Sequence""
        maxArchiveFiles=""30000""
        concurrentWrites=""true""
        keepFileOpen=""false""            
        >
    <layout xsi:type=""CSVLayout"">
    <column name =""sequence"" layout =""${counter}"" />
    <column name=""time"" layout=""${date:format=yyyy/MM/dd HH\:mm\:ss.fff}"" />
    <column name=""level"" layout=""${level}""/>
    <column name=""thread"" layout=""${threadid}""/>
    <column name=""class"" layout =""${callsite:className=true:methodName=false:fileName=false:includeSourcePath=false}"" />
    <column name=""method"" layout =""${callsite:className=false:methodName=true:fileName=false:includeSourcePath=false}"" />
    <column name=""message"" layout=""${message}"" />
    <column name=""exception"" layout=""${exception:format=Message,Type,StackTrace}"" />
    <column name=""file"" layout =""${callsite:className=false:methodName=false:fileName=true:includeSourcePath=true}"" />
    </layout>
</target>";

    public override bool IsAlive => this._fileReader is not null && this._fileWatcher is not null;

    public static void RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<CsvFileReceiver>();
        ReceiverSettingsDiscriminatorAttribute.Register<Settings>(registry.Collection);
    }

    public async Task FinishReadingAsync()
    {
        await (this._readFileStack ?? Task.CompletedTask);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Changed)
        {
            return;
        }

        this.BeginReadFile();
    }

    private async void BeginReadFile()
    {
        try
        {
            if (this._readFileStack is not null)
            {
                await this._readFileStack;
            }

            await (this._readFileStack = this.ReadFileAsync());
        }
        catch (Exception ex)
        {
            this.Notify(
                new LogMessage
                {
                    Level = LogLevel.Fatal,
                    Message = ex.Message,
                    ExceptionString = ex.ToString(),
                });
            Locator.Current.GetService<IObserver<Exception>>()?.OnNext(ex);
        }
    }

    private async Task ReadFileAsync()
    {
        if (this._fileReader is null)
        {
            return;
        }

        var fileReplaced = this._fileReader.BaseStream.Position > this._fileReader.BaseStream.Length;
        if (fileReplaced)
        {
            this._fileReader.BaseStream.Seek(0, SeekOrigin.Begin);
            this._fileReader.DiscardBufferedData();
        }

        // Get last added lines
        using var csv = new CsvReader(this._fileReader, this._fileReader.BaseStream.Position == 0L ? this._csvConfigurationBeginFile : this._csvConfigurationDuringFile, true);
        await foreach (var logMsg in csv.GetRecordsAsync<LogMessage>())
        {
            var fileName = logMsg.SourceFileName?.Trim("()".ToCharArray());
            //Detect the Line Nr
            if (fileName?.Split([":"], StringSplitOptions.None) is { Length: 3 } fileNameFields)
            {
                var lineNrString = fileNameFields[2];
                if (uint.TryParse(lineNrString, out var line))
                {
                    logMsg.SourceFileLineNr = line;
                }

                var realFileName = fileName![..(fileName.Length - lineNrString.Length - 1)];
                logMsg.SourceFileName = realFileName;
            }
            else
            {
                logMsg.SourceFileName = fileName;
            }

            // Notify the UI with the message
            this.Notify(logMsg);
        }
    }


    protected override void Initialize()
    {
        if (string.IsNullOrEmpty(this._settings.FileToWatch) || !this._fileSystem.File.Exists(this._settings.FileToWatch))
        {
            return;
        }

        this._fileReader = new StreamReader(this._fileSystem.FileStream.New(this._settings.FileToWatch, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

        var path = this._fileSystem.Path.GetDirectoryName(this._settings.FileToWatch)!;
        var filename = this._fileSystem.Path.GetFileName(this._settings.FileToWatch);
        this._fileWatcher = this._fileSystem.FileSystemWatcher.New(path, filename);
        this._fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
        this._fileWatcher.Changed += this.OnFileChanged;
        this._fileWatcher.EnableRaisingEvents = true;

        if (!this._settings.ShowFromBeginning)
        {
            this._fileReader.BaseStream.Seek(0, SeekOrigin.End);
            this._fileReader.DiscardBufferedData();
        }
    }

    protected override void Terminate()
    {
        if (this._fileWatcher is not null)
        {
            this._fileWatcher.EnableRaisingEvents = false;
            this._fileWatcher.Changed -= this.OnFileChanged;
            this._fileWatcher = null;
        }

        this._fileReader?.Close();
        this._fileReader = null;
    }

    protected override void OnAttached(ILogMessageNotifiable notifiable)
    {
        this.Attach(notifiable);

        if (this._settings.ShowFromBeginning)
        {
            this.BeginReadFile();
        }
    }

    [ReceiverSettingsDiscriminator(nameof(CsvFileReceiver), 1)]
    public record Settings() : ReceiverSettings(Settings.DefaultMappings)
    {
        private static readonly EquatableArray<FieldMapping> DefaultMappings =
        [
            new(LogMessageField.SequenceNr, "sequence"),
            new(LogMessageField.TimeStamp, "time"),
            new(LogMessageField.Level, "level"),
            new(LogMessageField.ThreadName, "thread"),
            new(LogMessageField.CallSiteClass, "class"),
            new(LogMessageField.CallSiteMethod, "method"),
            new(LogMessageField.Message, "message"),
            new(LogMessageField.Exception, "exception"),
            new(LogMessageField.SourceFileName, "file"),
        ];

        public override string Key => ReceiverSettings.CreateKey("Csv", this.GetLoggerName());
        public override string DisplayName => $"CSV {this.GetLoggerName()}";
        public override string TypeDisplayName => "CSV Log File";

        [Category("Configuration")]
        [DisplayName("File to Watch")]
        [PathBrowsable(Filters = "CSV Files(*.csv)|*.csv")]
        public string? FileToWatch
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

        [Category("Configuration")]
        [DisplayName("Has Header in File")]
        [Description("Read the Header or First List of the CSV File")]
        [DefaultValue(true)]
        public bool HasHeader
        {
            get;
            set => this.SetField(ref field, value);
        } = true;

        [Category("Configuration")]
        [DisplayName("Time Format")]
        [Description("Specifies the DateTime Format used to Parse the DateTime Field")]
        [DefaultValue("yyyy/MM/dd HH:mm:ss.fff")]
        public string DateTimeFormat
        {
            get;
            set => this.SetField(ref field, value);
        } = "yyyy/MM/dd HH:mm:ss.fff";

        [Category("Configuration")]
        [DisplayName("Quote Char")]
        [Description("If a field includes the delimiter, the whole field will be enclosed with a quote")]
        [DefaultValue("\"")]
        public string QuoteChar
        {
            get;
            set => this.SetField(ref field, value);
        } = "\"";

        [Category("Configuration")]
        [DisplayName("Delimiter ")]
        [Description("The character used to delimit each field")]
        [DefaultValue(",")]
        public string Delimiter
        {
            get;
            set => this.SetField(ref field, value);
        } = ",";

        [Category("Behavior")]
        [DisplayName("Logger Name")]
        [Description("Append the given Name to the Logger Name. If left empty, the filename will be used.")]
        public string? LoggerName
        {
            get;
            set => this.SetField(ref field, value);
        }

        public string GetLoggerName()
        {
            return (!string.IsNullOrWhiteSpace(this.LoggerName) ? this.LoggerName : Path.GetFileNameWithoutExtension(this.FileToWatch)) ?? "Csv";
        }

        public override ReceiverSettings DeepClone()
        {
            return this with { };
        }

        public override IReceiver CreateReceiver(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<CsvFileReceiver>(serviceProvider, this);
        }
    }
}
