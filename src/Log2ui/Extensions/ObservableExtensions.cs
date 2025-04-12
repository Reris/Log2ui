using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Log2ui.Collections.Observables;

namespace Log2ui.Extensions;

public static class ObservableExtensions
{
    public static IObservable<T> NotNull<T>(this IObservable<T?> observable)
    {
        return observable.Where(a => a is not null)!;
    }

    /// <summary>
    /// Helper to avoid a flickering on the settings editor, so it only deepclones a new setings when they changed from the outside.
    /// </summary>
    public static IObservable<T> SelectExceptCurrent<T>(this IObservable<T> observable, Func<T, T> select)
    {
        T current = default!;
        return observable.Select(
            a =>
            {
                if (object.ReferenceEquals(a, current))
                {
                    return current;
                }

                return current = select(a);
            });
    }

    public static ICurrentObservable<T> UseCurrent<T>(this IObservable<T> observable)
    {
        return new CurrentObservable<T>(observable.Replay(1).RefCount());
    }

    public static async ValueTask<T> GetCurrentAsync<T>(this IObservable<T> observable, CancellationToken cancellationToken = default)
    {
        if (observable is ICurrentObservable<T> { HasCurrent: true } currentObservable)
        {
            return currentObservable.Current;
        }

        return await observable.FirstAsync().ToTask(cancellationToken);
    }
}
