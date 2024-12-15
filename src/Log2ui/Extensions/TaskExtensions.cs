using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Log2ui.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="Task" /> class.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Syntaxvereinfachung für Inlining eines <see cref="Task{TIn}" /> um nur einen seiner Member abzufragen.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<TOut> SelectAsync<TIn, TOut>(this Task<TIn> task, Func<TIn, TOut> selector)
    {
        return selector(await task);
    }

    /// <summary>
    /// Syntaxvereinfachung für Inlining eines <see cref="ValueTask{TIn}" /> um nur einen seiner Member abzufragen.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async ValueTask<TOut> SelectAsync<TIn, TOut>(this ValueTask<TIn> task, Func<TIn, TOut> selector)
    {
        return selector(await task);
    }

    /// <summary>
    /// Task starten und nicht weiter beachten
    /// </summary>
    /// <param name="task">Der Task, der nicht weiter beachtet werden soll.</param>
    /// <param name="forgetExceptions">True wenn auch eine folgende Exception einfach verschluckt werden soll.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FireAndForget(this Task? task, bool forgetExceptions = false)
    {
        if (task is not null && !forgetExceptions)
        {
            task.ContinueWith(TaskExtensions.ThrowException, SynchronizationContext.Current, TaskContinuationOptions.OnlyOnFaulted);
        }
    }

    /// <summary>
    /// Task starten und nicht weiter beachten
    /// </summary>
    /// <typeparam name="TResult">Typ des Resultats.</typeparam>
    /// <param name="task">Der Task, der nicht weiter beachtet werden soll.</param>
    /// <param name="forgetExceptions">True wenn auch eine folgende Exception einfach verschluckt werden soll.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FireAndForget<TResult>(this Task<TResult>? task, bool forgetExceptions = false)
    {
        ((Task?)task).FireAndForget(forgetExceptions);
    }

    /// <summary>
    /// Task starten und nicht weiter beachten
    /// </summary>
    /// <param name="task">Der Task, der nicht weiter beachtet werden soll.</param>
    /// <param name="forgetExceptions">True wenn auch eine folgende Exception einfach verschluckt werden soll.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FireAndForget(this ValueTask task, bool forgetExceptions = false)
    {
        task.AsTask().FireAndForget(forgetExceptions);
    }

    /// <summary>
    /// Task starten und nicht weiter beachten
    /// </summary>
    /// <typeparam name="TResult">Typ des Resultats.</typeparam>
    /// <param name="task">Der Task, der nicht weiter beachtet werden soll.</param>
    /// <param name="forgetExceptions">True wenn auch eine folgende Exception einfach verschluckt werden soll.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FireAndForget<TResult>(this ValueTask<TResult> task, bool forgetExceptions = false)
    {
        task.AsTask().FireAndForget(forgetExceptions);
    }

    /// <summary>
    /// Losgelöstes await bei dem alle darauf folgenden Aktionen im <see cref="ThreadPool" /> stattfinden könnten.
    /// <see cref="Task.ConfigureAwait(bool)" />(false).
    /// </summary>
    /// <remarks>
    /// Könnten: Ist der Task bereits <see cref="Task.IsCompleted" /> (egal ob Success oder nicht), wird der Umweg auf einen WorkerThread eingespart
    /// und man bleibt also auf dem aktuellen, ggf UI-Thread.
    /// Tiefer erklärt: https://devblogs.microsoft.com/dotnet/configureawait-faq/
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConfiguredTaskAwaitable AwaitInPool(this Task? task)
    {
        return (task ?? Task.CompletedTask).ConfigureAwait(false);
    }

    /// <summary>
    /// Losgelöstes await bei dem alle darauf folgenden Aktionen im <see cref="ThreadPool" /> stattfinden könnten.
    /// <see cref="Task.ConfigureAwait(bool)" />(false).
    /// </summary>
    /// <remarks>
    /// Könnten: Ist der Task bereits <see cref="Task.IsCompleted" /> (egal ob Success oder nicht), wird der Umweg auf einen WorkerThread eingespart
    /// und man bleibt also auf dem aktuellen, ggf UI-Thread.
    /// Tiefer erklärt: https://devblogs.microsoft.com/dotnet/configureawait-faq/
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConfiguredTaskAwaitable<T> AwaitInPool<T>(this Task<T>? task)
    {
        return (task ?? Task.FromResult(default(T)!)).ConfigureAwait(false);
    }

    /// <summary>
    /// Losgelöstes await bei dem alle darauf folgenden Aktionen im <see cref="ThreadPool" /> stattfinden könnten.
    /// <see cref="Task.ConfigureAwait(bool)" />(false).
    /// </summary>
    /// <remarks>
    /// Könnten: Ist der Task bereits <see cref="Task.IsCompleted" /> (egal ob Success oder nicht), wird der Umweg auf einen WorkerThread eingespart
    /// und man bleibt also auf dem aktuellen, ggf UI-Thread.
    /// Tiefer erklärt: https://devblogs.microsoft.com/dotnet/configureawait-faq/
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConfiguredValueTaskAwaitable AwaitInPool(this ValueTask task)
    {
        return task.ConfigureAwait(false);
    }

    /// <summary>
    /// Losgelöstes await bei dem alle darauf folgenden Aktionen im <see cref="ThreadPool" /> stattfinden könnten.
    /// <see cref="Task.ConfigureAwait(bool)" />(false).
    /// </summary>
    /// <remarks>
    /// Könnten: Ist der Task bereits <see cref="Task.IsCompleted" /> (egal ob Success oder nicht), wird der Umweg auf einen WorkerThread eingespart
    /// und man bleibt also auf dem aktuellen, ggf UI-Thread.
    /// Tiefer erklärt: https://devblogs.microsoft.com/dotnet/configureawait-faq/
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConfiguredValueTaskAwaitable<T> AwaitInPool<T>(this ValueTask<T> task)
    {
        return task.ConfigureAwait(false);
    }

    /// <summary>
    /// Setzt alle folgenden Aufrufe in den <see cref="TaskScheduler.Default" />, also den <see cref="ThreadPool" />, sofern er das nicht schon ist.
    /// </summary>
    /// <remarks>
    /// Also eigentlich identisch zu einem Task.Run(()=>{}), nur dass es nicht erst umschalten muss.
    /// </remarks>
    public static ConfiguredTaskAwaitable AwaitInPool(this TaskFactory taskFactory, CancellationToken cancellationToken = default)
    {
        const TaskCreationOptions options = TaskCreationOptions.PreferFairness;
        cancellationToken.ThrowIfCancellationRequested(); // Cancel auch wenn kein Threadwechsel gemacht werden muss.
        return (taskFactory.Scheduler == TaskScheduler.Default
                    ? Task.CompletedTask // Wir sind bereits in einem Worker
                    : taskFactory.StartNew(cancellationToken.ThrowIfCancellationRequested, cancellationToken, options, TaskScheduler.Default))
            .AwaitInPool();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult WaitForResult<TResult>(this Task<TResult> task)
    {
        // Kein task.Wait(); task.Result:
        // https://stackoverflow.com/questions/36426937/what-is-the-difference-between-wait-vs-getawaiter-getresult
        return task.GetAwaiter().GetResult();
    }

    private static void ThrowException(Task completedTask, object? state)
    {
        if (state is not SynchronizationContext context)
        {
            return;
        }

        switch (completedTask.Exception?.GetBaseException())
        {
            case null:
                return;
            case OperationCanceledException:
                return;
            case { } ex:
                context.Send(TaskExtensions.ThrowExceptionInfo, ExceptionDispatchInfo.Capture(ex));
                return;
        }
    }

    private static void ThrowExceptionInfo(object? state)
    {
        if (state is ExceptionDispatchInfo ex)
        {
            ex.Throw();
        }
    }
}
