using System.Collections.Generic;
using Avalonia.ReactiveUI;
using Log2ui.Extensions;

namespace Log2ui.Views;

public class View<TViewModel> : ReactiveUserControl<TViewModel>, ViewExtensions.IInvoking
    where TViewModel : class
{
    IDictionary<string, IList<ViewExtensions.Invocation>> ViewExtensions.IInvoking.Invocations { get; }
        = new Dictionary<string, IList<ViewExtensions.Invocation>>();
}
