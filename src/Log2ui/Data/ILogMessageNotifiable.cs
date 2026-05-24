using System.Collections.Generic;

namespace Log2ui.Data;

public interface ILogMessageNotifiable
{
    void Notify(IReadOnlyList<LogMessage> messages);
}
