using System;

namespace Log2ui.Collections.Observables;

public interface ICurrentObservable<out T> : IObservable<T>
{
    bool HasCurrent { get; }
    T Current { get; }
}
