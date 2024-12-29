using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Log2ui.Extensions;

public static class ObservableExtensions
{
    public static IObservable<T> UseCurrent<T>(this IObservable<T> observable)
    {
        return observable.Replay().RefCount();
    }

    public static async ValueTask<T> GetCurrentAsync<T>(this IObservable<T> observable)
    {
        return await observable.FirstAsync();
    }
}
