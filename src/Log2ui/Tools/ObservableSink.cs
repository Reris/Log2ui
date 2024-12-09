using System;
using System.Reactive.Subjects;
using Serilog.Core;
using Serilog.Events;

namespace Log2ui.Tools;

public class ObservableSink : ILogEventSink, IObservable<LogEvent>, IDisposable
{
    private readonly Subject<LogEvent> _subject = new();

    public void Dispose()
    {
        this._subject.Dispose();
    }

    public void Emit(LogEvent logEvent)
    {
        this._subject.OnNext(logEvent);
    }

    public IDisposable Subscribe(IObserver<LogEvent> observer)
    {
        return this._subject.Subscribe(observer);
    }
}
