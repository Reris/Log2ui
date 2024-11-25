using System.Collections.Generic;

namespace Log2ui.Data;

public interface ILogManager
{
    void ClearAll();
    void ClearLogMessages();
    void DeactivateLogger();
    LogMessageItem ProcessLogMessage(LogMessage logMessage);
    IEnumerable<LogMessageItem> ProcessLogMessage(IEnumerable<LogMessage> logMessages);
    void SearchText(string value);
    void SetRootLoggerName(string name);
}
