using System;
using System.Reactive.Disposables;
using ReactiveUI;

namespace Log2ui.Views;

public class ViewModel : ReactiveObject, IViewModel, IDisposable
{
    protected CompositeDisposable Disposables { get; } = new();

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.Disposables.Dispose();
        }
    }
}
