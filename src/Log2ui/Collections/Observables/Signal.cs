using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;

namespace Log2ui.Collections.Observables;

public class Signal<T>()
{
    private T _current = default!;

    public Signal(T current)
        : this()
    {
        this.HasCurrent = true;
        this._current = current;
    }

    [MemberNotNullWhen(true, nameof(Signal<T>.Current))]
    public bool HasCurrent { get; private set; }

    public T Current => this.HasCurrent ? this._current : throw new NotEmittedException();
    public bool HasObservers => this.Next is not null;

    public event Action<T>? Next;

    public void OnNext(T obj)
    {
        this.HasCurrent = true;
        this._current = obj;
        this.Next?.Invoke(obj);
    }

    public IObservable<T> ToObservable()
    {
        var result = Observable.FromEvent<T>(a => this.Next += a, a => this.Next -= a);
        if (this.HasCurrent)
        {
            result = result.StartWith(this.Current);
        }

        return result;
    }
}
