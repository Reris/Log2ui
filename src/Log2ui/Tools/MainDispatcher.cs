using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Log2ui.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Tools;

public readonly struct MainDispatcher : IMainDispatcher, ISelfRegistering
{
    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddSingleton<IMainDispatcher>(new MainDispatcher(Dispatcher.UIThread));
    }

    private readonly Dispatcher _dispatcher;

    public MainDispatcher(Dispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        this._dispatcher = dispatcher;
    }

    public bool IsMainThread => this._dispatcher.CheckAccess();

    public Task InvokeAsync(Action callback, CancellationToken cancellationToken = default)
    {
        if (this.IsMainThread)
        {
            callback();
            return Task.CompletedTask;
        }

        return this._dispatcher.InvokeAsync(callback, default, cancellationToken).GetTask();
    }

    public Task InvokeAsync(Action callback, DispatcherPriority priority, CancellationToken cancellationToken = default)
    {
        if (this.IsMainThread)
        {
            callback();
            return Task.CompletedTask;
        }

        return this._dispatcher.InvokeAsync(callback, priority, cancellationToken).GetTask();
    }

    public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback, CancellationToken cancellationToken = default)
    {
        if (this.IsMainThread)
        {
            return Task.FromResult(callback());
        }

        return this._dispatcher.InvokeAsync(callback, default, cancellationToken).GetTask();
    }

    public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback, DispatcherPriority priority, CancellationToken cancellationToken = default)
    {
        if (this.IsMainThread)
        {
            return Task.FromResult(callback());
        }

        return this._dispatcher.InvokeAsync(callback, priority, cancellationToken).GetTask();
    }

    public Task InvokeAsync(Func<Task> callback, CancellationToken cancellationToken = default)
    {
        if (this.IsMainThread)
        {
            return Task.FromResult(callback());
        }

        return this._dispatcher.InvokeAsync(callback, default, cancellationToken).GetTask();
    }

    public Task InvokeAsync(Func<Task> callback, DispatcherPriority priority, CancellationToken cancellationToken = default)
    {
        if (this.IsMainThread)
        {
            return Task.FromResult(callback());
        }

        return this._dispatcher.InvokeAsync(callback, priority, cancellationToken).GetTask();
    }

    public async Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> callback, CancellationToken cancellationToken = default)
    {
        if (this.IsMainThread)
        {
            return await callback().ConfigureAwait(false);
        }

        return await (await this._dispatcher.InvokeAsync(callback, default, cancellationToken).GetTask().ConfigureAwait(false)).ConfigureAwait(false);
    }

    public async Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> callback, DispatcherPriority priority, CancellationToken cancellationToken = default)
    {
        if (this.IsMainThread)
        {
            return await callback().ConfigureAwait(false);
        }

        return await (await this._dispatcher.InvokeAsync(callback, priority, cancellationToken).GetTask().ConfigureAwait(false)).ConfigureAwait(false);
    }
}
