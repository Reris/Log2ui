using System;
using System.Reactive.Linq;

namespace Log2ui.Collections.Observables;

public class CurrentObservable<T> : ICurrentObservable<T>
{
    private readonly IObservable<T> _observable;
    private (bool HasValue, T Value) _current;

    public CurrentObservable(IObservable<T> observable)
    {
        this._observable = observable.Do(a => this._current = (true, a));
    }

    public bool HasValue => this._current.HasValue;
    public T Current => this.HasValue ? this._current.Value : throw new NotEmittedException();

    public IDisposable Subscribe(IObserver<T> observer)
    {
        return this._observable.Subscribe(observer);
    }

    public class NotEmittedException() : Exception("The Observable has not emitted a value yet.");
}
