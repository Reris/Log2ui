using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Log2ui.Tools;

public interface IMainDispatcher
{
    bool IsMainThread { get; }

    Task InvokeAsync(Action callback, CancellationToken cancellationToken = default);
    Task InvokeAsync(Action callback, DispatcherPriority priority, CancellationToken cancellationToken = default);
    Task<TResult> GetAsync<TResult>(Func<TResult> callback, CancellationToken cancellationToken = default);
    Task<TResult> GetAsync<TResult>(Func<TResult> callback, DispatcherPriority priority, CancellationToken cancellationToken = default);

    Task InvokeAsync(Func<CancellationToken, Task> callback, CancellationToken cancellationToken = default);
    Task InvokeAsync(Func<CancellationToken, Task> callback, DispatcherPriority priority, CancellationToken cancellationToken = default);
    Task<TResult> GetAsync<TResult>(Func<CancellationToken, Task<TResult>> callback, CancellationToken cancellationToken = default);

    Task<TResult> GetAsync<TResult>(
        Func<CancellationToken, Task<TResult>> callback,
        DispatcherPriority priority,
        CancellationToken cancellationToken = default);
}
