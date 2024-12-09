using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using Log2ui.Data;
using Log2ui.Settings;
using MsBox.Avalonia;

namespace Log2ui.Receivers;

/// <summary>
/// This receiver watch a given file, like a 'tail' program, with one log event by line.
/// Ideally the log events should use the log4j XML Schema layout.
/// </summary>
[Serializable]
[DisplayName("CSV Log File")]
public class CsvFileReceiver : BaseReceiver
{
    private string _dateTimeFormat = "yyyy/MM/dd HH:mm:ss.fff";

    private string _delimiter = ",";

    private FieldType[] _fieldList =
    {
        new(LogMessageField.SequenceNr, "sequence"),
        new(LogMessageField.TimeStamp, "time"),
        new(LogMessageField.Level, "level"),
        new(LogMessageField.ThreadName, "thread"),
        new(LogMessageField.CallSiteClass, "class"),
        new(LogMessageField.CallSiteMethod, "method"),
        new(LogMessageField.Message, "message"),
        new(LogMessageField.Exception, "exception"),
        new(LogMessageField.SourceFileName, "file")
    };

    [NonSerialized]
    private string? _filename;

    [NonSerialized]
    private StreamReader? _fileReader;

    private string _fileToWatch = string.Empty;

    [NonSerialized]
    private FileSystemWatcher? _fileWatcher;

    private string? _loggerName;

    private string _quoteChar = "\"";
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
    [DisplayName("Show from Beginning")]
    [Description("Show file contents from the beginning (not just newly appended lines)")]
    [DefaultValue(false)]
    public bool ShowFromBeginning
    {
        get => this._showFromBeginning;
        set => this._showFromBeginning = value;
    }

    [Category("Configuration")]
    [DisplayName("Field List")]
    [Description("Defines the type of each field")]
    public FieldType[] FieldList
    {
        get => this._fieldList;
        set => this._fieldList = value;
    }

    [Category("Configuration")]
    [DisplayName("Read Header From File")]
    [Description("Read the Header or First List of the CSV File to Automatically determine the Field Types")]
    [DefaultValue(false)]
    public bool ReadHeaderFromFile { get; set; }

    [Category("Configuration")]
    [DisplayName("Time Format")]
    [Description("Specifies the DateTime Format used to Parse the DateTime Field")]
    [DefaultValue("yyyy/MM/dd HH:mm:ss.fff")]
    public string DateTimeFormat
    {
        get => this._dateTimeFormat;
        set => this._dateTimeFormat = value;
    }

    [Category("Configuration")]
    [DisplayName("Quote Char")]
    [Description("If a field includes the delimiter, the whole field will be enclosed with a quote")]
    [DefaultValue("\"")]
    public string QuoteChar
    {
        get => this._quoteChar;
        set => this._quoteChar = value;
    }

    [Category("Configuration")]
    [DisplayName("Delimiter ")]
    [Description("The character used to delimit each field")]
    [DefaultValue(",")]
    public string Delimiter
    {
        get => this._delimiter;
        set => this._delimiter = value;
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


    private void Restart()
    {
        this.Terminate();
        this.Initialize();
    }

    private void ComputeFullLoggerName() => this.DisplayName = string.IsNullOrEmpty(this._loggerName)
                                                                   ? string.Empty
                                                                   : $"Log File [{this._loggerName}]";

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
        if (this._fileReader == null)
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

            if (fields.Count == this.FieldList.Length)
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
        for (var i = 0; i < this.FieldList.Length; i++)
        {
            var fieldType = this._fieldList[i];
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
                        logMsg.Level = LogLevels.Of(fieldValue);
                        //if (logMsg.Level == null)
                        //    throw new NullReferenceException("Cannot parse string: " + fieldValue);
                        break;
                    case LogMessageField.Message:
                        logMsg.Message = fieldValue;
                        break;
                    case LogMessageField.ThreadName:
                        logMsg.ThreadName = fieldValue;
                        break;
                    case LogMessageField.TimeStamp:
                        DateTime time;
                        DateTime.TryParseExact(fieldValue, this.DateTimeFormat, null, DateTimeStyles.None, out time);
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
                        var fileNameFields = fieldValue.Split(new[] { ":" }, StringSplitOptions.None);
                        if (fileNameFields.Length == 3)
                        {
                            uint line;
                            var lineNrString = fileNameFields[2];
                            if (uint.TryParse(lineNrString, out line))
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
                    sb.Append(this.Delimiter);
                }

                logMsg = new LogMessage
                {
                    SequenceNr = 0,
                    LoggerName = "Log2Console",
                    RootLoggerName = "Log2Console",
                    Level = LogLevels.Of(LogLevel.Error),
                    Message = "Error Parsing Log Entry Line: " + sb,
                    ThreadName = string.Empty,
                    TimeStamp = DateTime.Now,
                    ExceptionString = ex.Message + ex.StackTrace,
                    CallSiteClass = string.Empty,
                    CallSiteMethod = string.Empty,
                    SourceFileName = string.Empty,
                    SourceFileLineNr = 0
                };
                return;
            }
        }
    }

    private List<string> ReadLogEntry()
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
            if (line == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(line)) //Skip blank lines
            {
                continue;
            }

            var fields = line.Split(new[] { this.Delimiter }, StringSplitOptions.None);

            foreach (var nextField in fields)
            {
                //First check for Quote Char in fields
                if (!quoteDetected)
                {
                    //See if there is a start quote
                    if (nextField.Length > 0 && nextField.Substring(0, 1).Equals(this.QuoteChar))
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
                    if (nextField.Length > 0 && nextField.Substring(nextField.Length - 1, 1).Equals(this.QuoteChar))
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
                        quoteString.Append(
                            this.Delimiter); //Since this is enclosed in the Quote Char's it is part of a string field, and not valid delimiter                            
                    }
                }
            }

            //If this is a normal log entry, without any quotes, then check that the correct amount of fields is detected
            if (!quoteDetected && finalFields.Count != this.FieldList.Length)
            {
                return null;
            }
        } while (finalFields.Count < this.FieldList.Length); //If this is a multi line log, keep on reading the following lines

        return finalFields;
    }

    public override void Initialize()
    {
        if (string.IsNullOrEmpty(this._fileToWatch))
        {
            return;
        }

        this._fileReader =
            new StreamReader(new FileStream(this._fileToWatch, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

        var path = Path.GetDirectoryName(this._fileToWatch);
        this._filename = Path.GetFileName(this._fileToWatch);
        this._fileWatcher = new FileSystemWatcher(path, this._filename)
            { NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size };
        this._fileWatcher.Changed += this.OnFileChanged;
        this._fileWatcher.EnableRaisingEvents = true;

        this.ComputeFullLoggerName();

        if (this.ReadHeaderFromFile)
        {
            this.AutoConfigureHeader();
        }

        if (!this._showFromBeginning)
        {
            this._fileReader.BaseStream.Seek(0, SeekOrigin.End);
            this._fileReader.DiscardBufferedData();
        }
    }

    private void AutoConfigureHeader()
    {
        var line = this._fileReader.ReadLine();
        var fields = line.Split(new[] { this.Delimiter }, StringSplitOptions.None);
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
                this._fieldList = fieldList;
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

    public override void Terminate()
    {
        if (this._fileWatcher != null)
        {
            this._fileWatcher.EnableRaisingEvents = false;
            this._fileWatcher.Changed -= this.OnFileChanged;
            this._fileWatcher = null;
        }

        if (this._fileReader != null)
        {
            this._fileReader.Close();
        }

        this._fileReader = null;
    }

    protected override void OnAttached(ILogMessageNotifiable notifiable)
    {
        base.Attach(notifiable);

        if (this._showFromBeginning)
        {
            this.ReadFile();
        }
    }
}
