using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using Log2ui.Collections.Observables;

namespace Log2ui.Extensions;

public static class ObservableExtensions
{
    public static IObservable<T> NotNull<T>(this IObservable<T?> observable)
    {
        return observable.Where(a => a is not null)!;
    }

    extension<T>(IObservable<T> observable)
    {
        /// <summary>
        /// Helper to avoid a flickering on the settings editor, so it only deepclones a new settings when they changed from the outside.
        /// </summary>
        public IObservable<T> SelectExceptCurrent(Func<T, T> select)
        {
            T current = default!;
            return observable.Select(a =>
            {
                if (ReferenceEquals(a, current))
                {
                    return current;
                }

                return current = select(a);
            });
        }

        public ICurrentObservable<T> UseCurrent()
        {
            return new CurrentObservable<T>(observable.Replay(1).RefCount());
        }

        public async ValueTask<T> GetCurrentAsync(CancellationToken cancellationToken = default)
        {
            if (observable is ICurrentObservable<T> { HasCurrent: true } currentObservable)
            {
                return currentObservable.Current;
            }

            return await observable.FirstAsync().ToTask(cancellationToken);
        }

        public bool TryGetCurrent(int timeoutMs, [NotNullWhen(true)] out T found)
        {
            if (observable is ICurrentObservable<T> { HasCurrent: true } currentObservable)
            {
                found = currentObservable.Current!;
                return true;
            }

            var getTask = observable.FirstAsync().ToTask();
            if (Task.WhenAny(getTask, Task.Delay(timeoutMs)).GetAwaiter().GetResult() != getTask)
            {
                found = default!;
                return false;
            }

            found = getTask.GetAwaiter().GetResult()!;
            return true;
        }
    }

    extension<T>(IObservable<IReadOnlyCollection<T>> observable)
    {
        public ReadOnlyObservableCollection<T> ToObservableCollection(CompositeDisposable disposeWith)
        {
            var collection = new ObservableCollection<T>();
            observable
                .Subscribe(a =>
                {
                    collection.RemoveMany(collection.Except(a));
                    collection.AddRange(a.Except(collection));
                })
                .DisposeWith(disposeWith);

            return new ReadOnlyObservableCollection<T>(collection);
        }
    }
}
