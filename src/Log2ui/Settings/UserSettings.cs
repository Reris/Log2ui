using System;
using System.Collections.Generic;
using System.ComponentModel;
using Log2ui.Data;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Icon = MsBox.Avalonia.Enums.Icon;

namespace Log2ui.Settings;

[Serializable]
public class UserSettings
{
    private static readonly FieldType[] DefaultColumnConfiguration =
    {
        new(LogMessageField.TimeStamp, "Time"),
        new(LogMessageField.Level, "Level"),
        new(LogMessageField.RootLoggerName, "RootLoggerName"),
        new(LogMessageField.ThreadName, "Thread"),
        new(LogMessageField.Message, "Message"),
    };

    private static readonly FieldType[] DefaultDetailsMessageConfiguration =
    {
        new(LogMessageField.TimeStamp, "Time"),
        new(LogMessageField.Level, "Level"),
        new(LogMessageField.RootLoggerName, "RootLoggerName"),
        new(LogMessageField.ThreadName, "Thread"),
        new(LogMessageField.Message, "Message"),
    };

    private static readonly FieldType[] DefaultCsvColumnHeaderConfiguration =
    {
        new(LogMessageField.SequenceNr, "sequence"),
        new(LogMessageField.TimeStamp, "time"),
        new(LogMessageField.Level, "level"),
        new(LogMessageField.ThreadName, "thread"),
        new(LogMessageField.CallSiteClass, "class"),
        new(LogMessageField.CallSiteMethod, "method"),
        new(LogMessageField.Message, "message"),
        new(LogMessageField.Exception, "exception"),
        new(LogMessageField.SourceFileName, "file"),
    };

    private static UserSettings? _instance;
    private FieldType[]? _columnConfiguration;

    [NonSerialized]
    private Dictionary<string, int>? _columnProperties;

    private FieldType[]? _csvHeaderColumns;

    [NonSerialized]
    private Dictionary<string, FieldType>? _csvHeaderFieldTypes;

    private FieldType[]? _messageDetailConfiguration;

    private string _timeStampFormatString = "yyyy-MM-dd HH:mm:ss.ffff";


    private UserSettings()
    {
    }

    public static UserSettings Instance
    {
        get => UserSettings._instance ??= new UserSettings();
        set => UserSettings._instance = value;
    }

    [Category("Columns")]
    [DisplayName("Column Settings")]
    [Description("Configure which Columns to Display")]
    public FieldType[] ColumnConfiguration
    {
        get => this._columnConfiguration ?? (this.ColumnConfiguration = UserSettings.DefaultColumnConfiguration);
        set
        {
            this._columnConfiguration = value;
            this.ColumnProperties = this.UpdateColumnPropeties();
        }
    }


    [Category("Columns")]
    [DisplayName("CSV File Header Column Settings")]
    [Description("Configures which columns maps to which fields when auto detecting the CSV structure based on the header")]
    public FieldType[] CsvHeaderColumns
    {
        get => this._csvHeaderColumns ?? (this.CsvHeaderColumns = UserSettings.DefaultCsvColumnHeaderConfiguration);
        set
        {
            this._csvHeaderColumns = value;
            this.CsvHeaderFieldTypes = this.UpdateCsvColumnHeader();
        }
    }

    [Category("Logging")]
    [Description("Defines the format to be used to display the log message timestamps (cf. DateTime.ToString(format) in the .NET Framework.")]
    [DisplayName("TimeStamp Format String")]
    public string TimeStampFormatString
    {
        get => this._timeStampFormatString;
        set
        {
            try
            {
                _ = DateTime.Now.ToString(value); // If error, will throw FormatException
                this._timeStampFormatString = value;
            }
            catch (FormatException ex)
            {
                MessageBoxManager.GetMessageBoxStandard("Error Configuring Columns", ex.Message, ButtonEnum.Ok, Icon.Error);
                this._timeStampFormatString = "G"; // Back to default
            }
        }
    }

    [Category("Message Details")]
    [DisplayName("Details information")]
    [Description("Configure which information to Display in the message details")]
    public FieldType[] MessageDetailConfiguration
    {
        get => this._messageDetailConfiguration ?? (this.MessageDetailConfiguration = UserSettings.DefaultDetailsMessageConfiguration);
        set => this._messageDetailConfiguration = value;
    }

    [Browsable(false)]
    public Dictionary<string, int> ColumnProperties
    {
        get => this._columnProperties ??= this.UpdateColumnPropeties();
        set => this._columnProperties = value;
    }

    [Browsable(false)]
    public Dictionary<string, FieldType> CsvHeaderFieldTypes
    {
        get => this._csvHeaderFieldTypes ??= this.UpdateCsvColumnHeader();
        set => this._csvHeaderFieldTypes = value;
    }

    private Dictionary<string, int> UpdateColumnPropeties()
    {
        var result = new Dictionary<string, int>();
        for (var i = 0; i < this.ColumnConfiguration.Length; i++)
        {
            try
            {
                if (this.ColumnConfiguration[i].Field == LogMessageField.Properties)
                {
                    result.Add(this.ColumnConfiguration[i].Property, i);
                }
            }
            catch (Exception ex)
            {
                MessageBoxManager.GetMessageBoxStandard("Error Configuring Columns", ex.Message);
            }
        }

        return result;
    }

    private Dictionary<string, FieldType> UpdateCsvColumnHeader()
    {
        var result = new Dictionary<string, FieldType>();
        foreach (var column in this.CsvHeaderColumns)
        {
            result.Add(column.Name, column);
        }

        return result;
    }
}
