using System;
using Log2ui.Data;

namespace Log2ui.Views;

public interface ILogSearchViewModel
{
    Func<LogMessageItem, bool>? CurrentFilter { get; }
}
