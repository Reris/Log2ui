using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;

namespace Log2ui.Collections.Observables;

public sealed class Signal<T>() : SubjectBase<T>
{
    private readonly ReplaySubject<T> _subject = new(1);
    private T _current = default!;

    public Signal(T current)
        : this()
    {
        this.OnNext(current);
    }

    [MemberNotNullWhen(true, nameof(Signal<T>.Current))]
    public bool HasCurrent { get; private set; }

    public T Current => this.HasCurrent ? this._current : throw new NotEmittedException();
    public override bool HasObservers => this._subject.HasObservers;
    public override bool IsDisposed => this._subject.IsDisposed;

    public override void Dispose()
    {
        this._subject.Dispose();
    }

    public override void OnCompleted()
    {
        this._subject.OnCompleted();
    }

    public override void OnError(Exception error)
    {
        this.HasCurrent = false;
        this._subject.OnError(error);
    }

    public override void OnNext(T obj)
    {
        this.HasCurrent = true;
        this._current = obj;
        this._subject.OnNext(obj);
    }

    public override IDisposable Subscribe(IObserver<T> observer)
    {
        return this._subject.Subscribe(observer);
    }
}
