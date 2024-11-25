using System;

namespace Log2ui.Data;

[Serializable]
public enum LogLevel
{
    Invalid = -1,
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warn = 3,
    Error = 4,
    Fatal = 5
}
