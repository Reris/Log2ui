using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Log2ui.Extensions;

public static class ObservableExtensions
{
    public static IObservable<T> UseCurrent<T>(this IObservable<T> observable)
    {
        return observable.Replay(1).RefCount();
    }

    public static async ValueTask<T> GetCurrentAsync<T>(this IObservable<T> observable, CancellationToken cancellationToken = default)
    {
        return await observable.FirstAsync().ToTask(cancellationToken);
    }
}
