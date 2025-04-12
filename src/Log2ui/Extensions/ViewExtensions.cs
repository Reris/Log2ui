using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Threading;
using Log2ui.Collections;
using Log2ui.Views;

namespace Log2ui.Extensions;

public static class ViewExtensions
{
    public static void InvokeLatest<TViewModel>(
        this View<TViewModel> view,
        Func<TViewModel, Task> action,
        [CallerArgumentExpression(nameof(action))]
        string? key = null)
        where TViewModel : class, IViewModel
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(key);

        view.InvokeLatest(view.ViewModel, action, key);
    }

    public static void InvokeLatest<TViewModel>(
        this IInvoking invoking,
        TViewModel? viewModel,
        Func<TViewModel, Task> action,
        [CallerArgumentExpression(nameof(action))]
        string? key = null)
        where TViewModel : class
    {
        ArgumentNullException.ThrowIfNull(invoking);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(key);

        if (viewModel is not null)
        {
            if (!invoking.Invocations.TryGetValue(key, out var queue))
            {
                invoking.Invocations[key] = queue = [];
            }

            queue.Add(new Invocation(() => action(viewModel)));
            Dispatcher.UIThread.Invoke(
                async () =>
                {
                    var running = queue.Select(a => a.Running).NotNull().ToArray();
                    var last = queue.LastOrDefault();
                    if (last is not null && last.Running is null)
                    {
                        await Task.WhenAll(running);
                        await (last.Running ??= last.Run());
                        queue.Clear();
                    }
                });
        }
    }

    public interface IInvoking
    {
        IDictionary<string, IList<Invocation>> Invocations { get; }
    }

    public class Invocation(Func<Task> run)
    {
        public Func<Task> Run => run;
        public Task? Running { get; set; }
    }
}
