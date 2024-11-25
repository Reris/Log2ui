using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using HarfBuzzSharp;
using Log2ui.Data;
using Log2ui.Receivers;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Icon = MsBox.Avalonia.Enums.Icon;

namespace Log2ui.Settings;

[Serializable]
public sealed class UserSettings
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

    private LogLevelInfo _logLevelInfo;

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
        // Set default values
        this._logLevelInfo = LogLevels.Of(LogLevel.Trace);
    }

    public static UserSettings Instance
    {
        get => UserSettings._instance ??= new UserSettings();
        set => UserSettings._instance = value;
    }

    [Category("Appearance")]
    [Description("Hides the taskbar icon, only the tray icon will remain visible.")]
    [DisplayName("Hide Taskbar Icon")]
    public bool HideTaskbarIcon
    {
        get => this._hideTaskbarIcon;
        set => this._hideTaskbarIcon = value;
    }

    [Category("Appearance")]
    [Description("The Log2Console window will remain on top of all other windows.")]
    [DisplayName("Always On Top")]
    public bool AlwaysOnTop
    {
        get => this._alwaysOnTop;
        set => this._alwaysOnTop = value;
    }

    [Category("Appearance")]
    [Description("Select a transparency factor for the main window.")]
    public uint Transparency
    {
        get => this._transparency;
        set => this._transparency = Math.Max(10, Math.Min(100, value));
    }

    [Category("Appearance")]
    [Description("Highlight the Logger of the selected Log Message.")]
    [DisplayName("Highlight Logger")]
    public bool HighlightLogger
    {
        get => this._highlightLogger;
        set => this._highlightLogger = value;
    }

    [Category("Appearance")]
    [Description("Highlight the Log Messages of the selected Logger.")]
    [DisplayName("Highlight Log Messages")]
    public bool HighlightLogMessages
    {
        get => this._highlightLogMessages;
        set => this._highlightLogMessages = value;
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


    [Category("Notification")]
    [Description("A balloon tip will be displayed when a new log message arrives and the window is hidden.")]
    [DisplayName("Notify New Log When Hidden")]
    public bool NotifyNewLogWhenHidden
    {
        get => this._notifyNewLogWhenHidden;
        set => this._notifyNewLogWhenHidden = value;
    }

    [Category("Notification")]
    [Description("Automatically scroll to the last log message.")]
    [DisplayName("Auto Scroll to Last Log")]
    public bool AutoScrollToLastLog
    {
        get => this._autoScrollToLastLog;
        set => this._autoScrollToLastLog = value;
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

    [Category("Message Details")]
    [Description("Show or hide the message properties in the message details panel.")]
    [DisplayName("Show Properties")]
    public bool ShowMsgDetailsProperties
    {
        get => this._msgDetailsProperties;
        set => this._msgDetailsProperties = value;
    }

    [Category("Message Details")]
    [Description("Show or hide the exception in the message details panel.")]
    [DisplayName("Show Exception")]
    public bool ShowMsgDetailsException
    {
        get => this._msgDetailsException;
        set => this._msgDetailsException = value;
    }

    [Category("Fonts")]
    [Description("Set the default Font.")]
    [DisplayName("Default Font")]
    public Font DefaultFont
    {
        get => this._defaultFont;
        set => this._defaultFont = value;
    }

    [Category("Fonts")]
    [Description("Set the Font of the Log List View.")]
    [DisplayName("Log List View Font")]
    public Font LogListFont
    {
        get => this._logListFont;
        set => this._logListFont = value;
    }

    [Category("Fonts")]
    [Description("Set the Font of the Log Detail View.")]
    [DisplayName("Log Detail View Font")]
    public Font LogDetailFont
    {
        get => this._logDetailFont;
        set => this._logDetailFont = value;
    }

    [Category("Fonts")]
    [Description("Set the Font of the Logger Tree.")]
    [DisplayName("Logger Tree Font")]
    public Font LoggerTreeFont
    {
        get => this._loggerTreeFont;
        set => this._loggerTreeFont = value;
    }

    [Category("Colors")]
    [Description("Set the Background Color of the Log List View.")]
    [DisplayName("Log List View Background Color")]
    public Color LogListBackColor
    {
        get => this._logListBackColor;
        set => this._logListBackColor = value;
    }

    [Category("Colors")]
    [Description("Set the Background Color of the Log Message details.")]
    [DisplayName("Log Message details Background Color")]
    public Color LogMessageBackColor
    {
        get => this._logMessageBackColor;
        set => this._logMessageBackColor = value;
    }


    [Category("Log Level Colors")]
    [DisplayName("1 - Trace Level Color")]
    public Color? TraceLevelColor
    {
        get => this._traceLevelColor;
        set => this._traceLevelColor = value;
    }

    [Category("Log Level Colors")]
    [DisplayName("2 - Debug Level Color")]
    public Color? DebugLevelColor
    {
        get => this._debugLevelColor;
        set => this._debugLevelColor = value;
    }

    [Category("Log Level Colors")]
    [DisplayName("3 - Info Level Color")]
    public Color? InfoLevelColor
    {
        get => this._infoLevelColor;
        set => this._infoLevelColor = value;
    }

    [Category("Log Level Colors")]
    [DisplayName("4 - Warning Level Color")]
    public Color? WarnLevelColor
    {
        get => this._warnLevelColor;
        set => this._warnLevelColor = value;
    }

    [Category("Log Level Colors")]
    [DisplayName("5 - Error Level Color")]
    public Color? ErrorLevelColor
    {
        get => this._errorLevelColor;
        set => this._errorLevelColor = value;
    }

    [Category("Log Level Colors")]
    [DisplayName("6 - Fatal Level Color")]
    public Color? FatalLevelColor
    {
        get => this._fatalLevelColor;
        set => this._fatalLevelColor = value;
    }


    /// <summary>
    /// This setting is not available through the Settings PropertyGrid.
    /// </summary>
    [Browsable(false)]
    internal LogLevelInfo LogLevelInfo
    {
        get => this._logLevelInfo;
        set => this._logLevelInfo = value;
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
        foreach (var receiver in this._receivers)
        {
            receiver.Terminate();
        }

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
