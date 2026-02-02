using System.Collections.Generic;

namespace Log2ui.Data;

public interface ILogManager
{
    void ClearAll();
    void ClearLogMessages();
    void DeactivateLogger();
    void ProcessLogMessage(LogMessage logMessage);
    void ProcessLogMessage(IEnumerable<LogMessage> logMessages);
    void SearchText(string value);
    void SetRootLoggerName(string name);
}
