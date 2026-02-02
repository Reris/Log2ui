using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Log2ui.Data;

namespace Log2ui.Exporters;

public interface IExport
{
    Task ExportAsync(IEnumerable<LogMessage> logMessages, CancellationToken cancellationToken = default);
}
