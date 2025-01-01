using System;

namespace Log2ui.Views;

public interface ICaptionedViewModel : IViewModel
{
    IObservable<string> Caption { get; }
}
