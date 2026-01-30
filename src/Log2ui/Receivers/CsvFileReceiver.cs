using Log2ui.Collections;
using Log2ui.Data;
using Log2ui.Dependencies;
using Log2ui.Settings;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;

namespace Log2ui.Receivers;

/// <summary>
/// This receiver watch a given file, like a 'tail' program, with one log event by line.
/// Ideally the log events should use the log4j XML Schema layout.
/// </summary>
[DisplayName("CSV Log File")]
public class CsvFileReceiver(CsvFileReceiver.Settings settings) : BaseReceiver, ISelfRegistering
{
    public static void RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<CsvFileReceiver>();
        ReceiverSettingsDiscriminatorAttribute.Register<Settings>(registry.Collection);
    }

    [ReceiverSettingsDiscriminator(nameof(CsvFileReceiver), 1)]
    public record Settings() : ReceiverSettings(Settings.DefaultProperties)
    {
        private static readonly EquatableArray<LogColumn> DefaultProperties = [];

        public override string Key => ReceiverSettings.CreateKey<CsvFileReceiver>(this.GetLoggerName());
        public override string DisplayName => $"CSV {this.GetLoggerName()}";
        public override string TypeDisplayName => "CSV Log File";

        [Category("Configuration")]
        [DisplayName("File to Watch")]
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
        [DisplayName("Field List")]
        [Description("Defines the type of each field")]
        public EquatableArray<FieldType> FieldList
        {
            get;
            set => this.SetField(ref field, value);
        } =
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

        [Category("Configuration")]
        [DisplayName("Read Header From File")]
        [Description("Read the Header or First List of the CSV File to Automatically determine the Field Types")]
        [DefaultValue(false)]
        public bool ReadHeaderFromFile
        {
            get;
            set => this.SetField(ref field, value);
        }

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
            set=> this.SetField(ref field, value);
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

    [NonSerialized]
    private string? _filename;

    [NonSerialized]
    private StreamReader? _fileReader;

    private string _fileToWatch = string.Empty;

    [NonSerialized]
    private FileSystemWatcher? _fileWatcher;

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
        if (this._fileReader is null)
        {
            return;
        }

        if (this._fileReader.BaseStream.Position > this._fileReader.BaseStream.Length)
        {
            this._fileReader.BaseStream.Seek(0, SeekOrigin.Begin);
            this._fileReader.DiscardBufferedData();
        }

        // Get last added lines
        var logMsgs = new List<LogMessage>();

        while (this.ReadLogEntry() is { } fields)
        {
            var logMsg = new LogMessage { ThreadName = string.Empty };

            if (fields.Count == settings.FieldList.Count)
            {
                this.ParseFields(ref logMsg, fields);
                logMsgs.Add(logMsg);
            }
        }

        // Notify the UI with the set of messages
        this.Notify(logMsgs);
    }

    private void ParseFields(ref LogMessage logMsg, List<string> fields)
    {
        for (var i = 0; i < settings.FieldList.Count; i++)
        {
            var fieldType = settings.FieldList[i];
            var fieldValue = fields[i];
            try
            {
                switch (fieldType.Field)
                {
                    case LogMessageField.SequenceNr:
                        logMsg.SequenceNr = ulong.Parse(fieldValue);
                        break;
                    case LogMessageField.LoggerName:
                        logMsg.LoggerName = fieldValue;
                        break;
                    case LogMessageField.RootLoggerName:
                        logMsg.RootLoggerName = fieldValue;
                        break;
                    case LogMessageField.Level:
                        logMsg.Level = Enum.TryParse<LogLevel>(fieldValue, true, out var level) ? level : LogLevel.Invalid;
                        break;
                    case LogMessageField.Message:
                        logMsg.Message = fieldValue;
                        break;
                    case LogMessageField.ThreadName:
                        logMsg.ThreadName = fieldValue;
                        break;
                    case LogMessageField.TimeStamp:
                        DateTime.TryParseExact(fieldValue, settings.DateTimeFormat, null, DateTimeStyles.None, out var time);
                        logMsg.TimeStamp = time;
                        break;
                    case LogMessageField.Exception:
                        logMsg.ExceptionString = fieldValue;
                        break;
                    case LogMessageField.CallSiteClass:
                        logMsg.CallSiteClass = fieldValue;
                        logMsg.LoggerName = logMsg.CallSiteClass;
                        break;
                    case LogMessageField.CallSiteMethod:
                        logMsg.CallSiteMethod = fieldValue;
                        break;
                    case LogMessageField.SourceFileName:
                        fieldValue = fieldValue.Trim("()".ToCharArray());
                        //Detect the Line Nr
                        var fileNameFields = fieldValue.Split([":"], StringSplitOptions.None);
                        if (fileNameFields.Length == 3)
                        {
                            var lineNrString = fileNameFields[2];
                            if (uint.TryParse(lineNrString, out var line))
                            {
                                logMsg.SourceFileLineNr = line;
                            }

                            var fileName = fieldValue.Substring(0, fieldValue.Length - lineNrString.Length - 1);
                            logMsg.SourceFileName = fileName;
                        }
                        else
                        {
                            logMsg.SourceFileName = fieldValue;
                        }

                        break;
                    case LogMessageField.SourceFileLineNr:
                        logMsg.SourceFileLineNr = uint.Parse(fieldValue);
                        break;
                    case LogMessageField.Properties:
                        logMsg.Properties.Add(fieldType.Property, fieldValue);
                        break;
                }
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                foreach (var field in fields)
                {
                    sb.Append(field);
                    sb.Append(settings.Delimiter);
                }

                logMsg = new LogMessage
                {
                    SequenceNr = 0,
                    LoggerName = "Log2ui",
                    RootLoggerName = "Log2ui",
                    Level = LogLevel.Error,
                    Message = "Error Parsing Log Entry Line: " + sb,
                    ThreadName = string.Empty,
                    TimeStamp = DateTime.Now,
                    ExceptionString = ex.Message + ex.StackTrace,
                    CallSiteClass = string.Empty,
                    CallSiteMethod = string.Empty,
                    SourceFileName = string.Empty,
                    SourceFileLineNr = 0,
                };
                return;
            }
        }
    }

    private List<string>? ReadLogEntry()
    {
        var finalFields = new List<string>();
        var quoteDetected = false;
        StringBuilder? quoteString = null;

        do
        {
            //If there is a log entry, that spans multiple lines, it will be surrounded by the Quote Character. 
            if (quoteDetected)
            {
                quoteString.AppendLine();
            }

            var line = this._fileReader.ReadLine();
            if (line is null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(line)) //Skip blank lines
            {
                continue;
            }

            var fields = line.Split([settings.Delimiter], StringSplitOptions.None);

            foreach (var nextField in fields)
            {
                //First check for Quote Char in fields
                if (!quoteDetected)
                {
                    //See if there is a start quote
                    if (nextField.Length > 0 && nextField.Substring(0, 1).Equals(settings.QuoteChar))
                    {
                        quoteString = new StringBuilder();
                        if (nextField.Length > 1)
                        {
                            var fieldWithoutQuote = nextField.Substring(1, nextField.Length - 1);
                            quoteString.Append(fieldWithoutQuote);
                            quoteDetected = true;
                        }
                    }
                    //If not, simply add the field
                    else
                    {
                        finalFields.Add(nextField);
                    }
                }
                //Keep on concatenating the string until the end quote is detected
                else
                {
                    //See if the last character is a quote                        
                    if (nextField.Length > 0 && nextField.Substring(nextField.Length - 1, 1).Equals(settings.QuoteChar))
                    {
                        var fieldWithoutQuote = nextField.Substring(0, nextField.Length - 1);
                        quoteString.Append(fieldWithoutQuote);
                        quoteDetected = false;
                        finalFields.Add(quoteString.ToString());
                    }
                    //No quote is detected, keep on adding the next field
                    else
                    {
                        quoteString.Append(nextField);
                        quoteString.Append(settings.Delimiter); //Since this is enclosed in the Quote Char's it is part of a string field, and not valid delimiter                            
                    }
                }
            }

            //If this is a normal log entry, without any quotes, then check that the correct amount of fields is detected
            if (!quoteDetected && finalFields.Count != settings.FieldList.Count)
            {
                return null;
            }
        } while (finalFields.Count < settings.FieldList.Count); //If this is a multi line log, keep on reading the following lines

        return finalFields;
    }

    protected override void Initialize()
    {
        if (string.IsNullOrEmpty(this._fileToWatch))
        {
            return;
        }

        this._fileReader = new StreamReader(new FileStream(this._fileToWatch, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

        var path = Path.GetDirectoryName(this._fileToWatch);
        this._filename = Path.GetFileName(this._fileToWatch);
        this._fileWatcher = new FileSystemWatcher(path, this._filename)
            { NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size };
        this._fileWatcher.Changed += this.OnFileChanged;
        this._fileWatcher.EnableRaisingEvents = true;

        if (settings.ReadHeaderFromFile)
        {
            this.AutoConfigureHeader();
        }

        if (!settings.ShowFromBeginning)
        {
            this._fileReader.BaseStream.Seek(0, SeekOrigin.End);
            this._fileReader.DiscardBufferedData();
        }
    }

    private void AutoConfigureHeader()
    {
        var line = this._fileReader.ReadLine();
        var fields = line.Split([settings.Delimiter], StringSplitOptions.None);
        var headerValid = false;
        try
        {
            var fieldList = new FieldType[fields.Length];
            for (var index = 0; index < fields.Length; index++)
            {
                var field = fields[index];

                if (UserSettings.Instance.CsvHeaderFieldTypes.ContainsKey(field))
                {
                    fieldList[index] = UserSettings.Instance.CsvHeaderFieldTypes[field];

                    //Note: This is a very basic check for a valid header. If any field is detected, the header
                    //is considered valid. This could be made more thorough. 
                    headerValid = true;
                }
                else
                {
                    fieldList[index] = new FieldType(LogMessageField.Properties, field, field);
                }
            }

            if (headerValid)
            {
                settings.FieldList = fieldList;
            }
            else
            {
                MessageBoxManager.GetMessageBoxStandard("Error Parsing CSV Header", "Could not Parse the Header: " + line);
            }
        }
        catch (Exception ex)
        {
            MessageBoxManager.GetMessageBoxStandard(
                "Error Parsing CSV Header",
                $"Could not Parse the Header: {line}{Environment.NewLine}Error: {ex}");
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

        if (this._fileReader is not null)
        {
            this._fileReader.Close();
        }

        this._fileReader = null;
    }

    protected override void OnAttached(ILogMessageNotifiable notifiable)
    {
        this.Attach(notifiable);

        if (settings.ShowFromBeginning)
        {
            this.ReadFile();
        }
    }
}
