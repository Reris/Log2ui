using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Log2ui.Helpers;

public static class TaskExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FireAndForget(this Task? task, bool forgetExceptions = false)
    {
        if (task is not null && !forgetExceptions)
        {
            task.ContinueWith(TaskExtensions.ThrowException, SynchronizationContext.Current, TaskContinuationOptions.OnlyOnFaulted);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FireAndForget(this ValueTask task, bool forgetExceptions = false) => task.AsTask().FireAndForget(forgetExceptions);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FireAndForget<TResult>(this ValueTask<TResult> task, bool forgetExceptions = false) => task.AsTask().FireAndForget(forgetExceptions);

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
