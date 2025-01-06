using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using Log2ui.Data;
using Log2ui.Receivers;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Font = HarfBuzzSharp.Font;
using Icon = MsBox.Avalonia.Enums.Icon;

namespace Log2ui.Settings;

[Serializable]
public class UserSettings
{
    [NonSerialized]
    private const string SettingsFileName = "UserSettings.dat";

    public class DefaultLight
    {
        public static readonly Color TraceLevelColor = Color.Gray;
        public static readonly Color DebugLevelColor = Color.White;
        public static readonly Color InfoLevelColor = Color.Green;
        public static readonly Color WarnLevelColor = Color.Orange;
        public static readonly Color ErrorLevelColor = Color.Red;
        public static readonly Color FatalLevelColor = Color.Purple;
    }

    public class DefaultDark
    {
        public static readonly Color TraceLevelColor = Color.Gray;
        public static readonly Color DebugLevelColor = Color.Black;
        public static readonly Color InfoLevelColor = Color.Green;
        public static readonly Color WarnLevelColor = Color.Orange;
        public static readonly Color ErrorLevelColor = Color.Red;
        public static readonly Color FatalLevelColor = Color.Purple;
    }

    private static readonly FieldType[] DefaultColumnConfiguration =
    {
        new(LogMessageField.TimeStamp, "Time"),
        new(LogMessageField.Level, "Level"),
        new(LogMessageField.RootLoggerName, "RootLoggerName"),
        new(LogMessageField.ThreadName, "Thread"),
        new(LogMessageField.Message, "Message")
    };

    private static readonly FieldType[] DefaultDetailsMessageConfiguration =
    {
        new(LogMessageField.TimeStamp, "Time"),
        new(LogMessageField.Level, "Level"),
        new(LogMessageField.RootLoggerName, "RootLoggerName"),
        new(LogMessageField.ThreadName, "Thread"),
        new(LogMessageField.Message, "Message")
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
        new(LogMessageField.SourceFileName, "file")
    };

    private static UserSettings? _instance;
    private bool _alwaysOnTop;
    private bool _autoScrollToLastLog = true;
    private FieldType[]? _columnConfiguration;

    [NonSerialized]
    private Dictionary<string, int>? _columnProperties;

    private FieldType[]? _csvHeaderColumns;

    [NonSerialized]
    private Dictionary<string, FieldType>? _csvHeaderFieldTypes;

    private Color? _traceLevelColor;
    private Color? _debugLevelColor;
    private Color? _infoLevelColor;
    private Color? _warnLevelColor;
    private Color? _errorLevelColor;
    private Color? _fatalLevelColor;
    
    private Font? _defaultFont;
    private bool _hideTaskbarIcon;
    private bool _highlightLogger = true;
    private bool _highlightLogMessages = true;
    private LayoutSettings _layout = new();
    private Font? _logDetailFont;
    private Font? _loggerTreeFont;

    private Color _logListBackColor = Color.Empty;
    private Font? _logListFont;
    private Color _logMessageBackColor = Color.Empty;
    private int _messageCycleCount;
    private FieldType[]? _messageDetailConfiguration;
    private bool _msgDetailsException = true;

    private bool _msgDetailsProperties;
    private bool _notifyNewLogWhenHidden = true;
    private List<IReceiver> _receivers = new();

    private bool _recursivlyEnableLoggers = true;

    [NonSerialized]
    private Dictionary<string, string>? _sourceFileLocationMap;

    private SourceFileLocation[]? _sourceLocationMapConfiguration;
    private string _timeStampFormatString = "yyyy-MM-dd HH:mm:ss.ffff";

    private uint _transparency = 100;


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

    [Category("Source File Configuration")]
    [DisplayName("Source Location")]
    [Description("Map the Log File Location to the Local Source Code Location")]
    public SourceFileLocation[] SourceLocationMapConfiguration
    {
        get => this._sourceLocationMapConfiguration;
        set
        {
            this._sourceLocationMapConfiguration = value;
            this.SourceFileLocationMap = this.UpdateSourceFileLocationMap();
        }
    }


    [Category("Logging")]
    [Description("When greater than 0, the log messages are limited to that number.")]
    [DisplayName("Message Cycle Count")]
    public int MessageCycleCount
    {
        get => this._messageCycleCount;
        set => this._messageCycleCount = value;
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

    [Category("Logging")]
    [Description("When a logger is enabled or disabled, do the same for all child loggers.")]
    [DisplayName("Recursively Enable Loggers")]
    public bool RecursivlyEnableLoggers
    {
        get => this._recursivlyEnableLoggers;
        set => this._recursivlyEnableLoggers = value;
    }

    [Category("Message Details")]
    [DisplayName("Details information")]
    [Description("Configure which information to Display in the message details")]
    public FieldType[] MessageDetailConfiguration
    {
        get => this._messageDetailConfiguration ?? (this.MessageDetailConfiguration = UserSettings.DefaultDetailsMessageConfiguration);
        set => this._messageDetailConfiguration = value;
    }

    /// <summary>
    /// This setting is not available through the Settings PropertyGrid.
    /// </summary>
    [Browsable(false)]
    internal List<IReceiver> Receivers
    {
        get => this._receivers;
        set => this._receivers = value;
    }

    /// <summary>
    /// This setting is not available through the Settings PropertyGrid.
    /// </summary>
    [Browsable(false)]
    internal LayoutSettings Layout
    {
        get => this._layout;
        set => this._layout = value;
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

    [Browsable(false)]
    public Dictionary<string, string> SourceFileLocationMap
    {
        get => this._sourceFileLocationMap ??= this.UpdateSourceFileLocationMap();
        set => this._sourceFileLocationMap = value;
    }

    /// <summary>
    /// Creates and returns an exact copy of the settings.
    /// </summary>
    /// <returns></returns>
    public UserSettings Clone()
    {
        // Clone via serialize
        var data = JsonSerializer.Serialize(this, JsonSerializerOptions.Default);
        return JsonSerializer.Deserialize<UserSettings>(data, JsonSerializerOptions.Default) ?? throw new SerializationException();
    }

    public static bool Load()
    {
        UserSettings._instance = new UserSettings();

        var settingsFilePath = UserSettings.GetSettingsFilePath();
        if (!File.Exists(settingsFilePath))
        {
            return false;
        }

        try
        {
            using var fs = new FileStream(settingsFilePath, FileMode.Open);
            if (fs.Length > 0)
            {
                UserSettings._instance = JsonSerializer.Deserialize<UserSettings>(fs, JsonSerializerOptions.Default);
                return true;
            }

            return false;
        }
        catch (Exception)
        {
            // The settings file might be corrupted or from too different version, delete it...
            try
            {
                File.Delete(settingsFilePath);
            }
            catch
            {
                return false;
            }

            return false;
        }
    }

    private static string GetSettingsFilePath()
    {
        var userDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var di = new DirectoryInfo(userDir);
        di = di.CreateSubdirectory("Log2Console");

        return di.FullName + Path.DirectorySeparatorChar + UserSettings.SettingsFileName;
    }

    public void Save()
    {
        var settingsFilePath = UserSettings.GetSettingsFilePath();

        using var fs = new FileStream(settingsFilePath, FileMode.Create);
        JsonSerializer.Serialize(fs, this, JsonSerializerOptions.Default);
    }

    public void Close()
    {
        this._receivers.Clear();
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

    private Dictionary<string, string> UpdateSourceFileLocationMap()
    {
        var result = new Dictionary<string, string>();
        foreach (var map in this.SourceLocationMapConfiguration)
        {
            result.Add(map.LogSource, map.LocalSource);
        }

        return result;
    }
}
