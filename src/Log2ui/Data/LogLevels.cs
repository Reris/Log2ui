using System;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using Log2ui.Settings;

namespace Log2ui.Data;

public static class LogLevels
{
    private static LogLevelInfo? _invalid;
    private static ImmutableArray<LogLevelInfo>? _all;

    public static LogLevelInfo Invalid => LogLevels._invalid ?? throw new NotInitializedException();
    public static ImmutableArray<LogLevelInfo> All => LogLevels._all ?? throw new NotInitializedException();

    public static void Init()
    {
        LogLevels._invalid = new LogLevelInfo(LogLevel.Invalid, Color.IndianRed);
        LogLevels._all = ImmutableArray.Create(
            new LogLevelInfo(LogLevel.Trace, nameof(LogLevel.Trace), UserSettings.DefaultLight.TraceLevelColor, UserSettings.DefaultDark.TraceLevelColor),
            new LogLevelInfo(LogLevel.Debug, nameof(LogLevel.Debug), UserSettings.DefaultLight.DebugLevelColor, UserSettings.DefaultDark.DebugLevelColor),
            new LogLevelInfo(LogLevel.Info, nameof(LogLevel.Info), UserSettings.DefaultLight.InfoLevelColor, UserSettings.DefaultDark.InfoLevelColor),
            new LogLevelInfo(LogLevel.Warn, nameof(LogLevel.Warn), UserSettings.DefaultLight.WarnLevelColor, UserSettings.DefaultDark.WarnLevelColor),
            new LogLevelInfo(LogLevel.Error, nameof(LogLevel.Error), UserSettings.DefaultLight.ErrorLevelColor, UserSettings.DefaultDark.ErrorLevelColor),
            new LogLevelInfo(LogLevel.Fatal, nameof(LogLevel.Fatal), UserSettings.DefaultLight.FatalLevelColor, UserSettings.DefaultDark.FatalLevelColor));
    }

    public static LogLevelInfo Of(int level)
    {
        if (level is < (int)LogLevel.Trace or > (int)LogLevel.Fatal)
        {
            return LogLevels.Invalid;
        }

        return LogLevels.All[level];
    }

    public static LogLevelInfo Of(LogLevel logLevel)
    {
        var level = (int)logLevel;
        if (level is < (int)LogLevel.Trace or > (int)LogLevel.Fatal)
        {
            return LogLevels.Invalid;
        }

        return LogLevels.All[level];
    }

    public static LogLevelInfo Of(string level)
    {
        foreach (var info in LogLevels.All.Where(info => info.Name.Equals(level, StringComparison.InvariantCultureIgnoreCase)))
        {
            return info;
        }

        return LogLevels.Invalid;
    }
}
