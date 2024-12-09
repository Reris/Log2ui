using System;
using Avalonia;
using Avalonia.ReactiveUI;
using Log2ui.Extensions;
using Log2ui.Tools;
using Serilog;
using Serilog.Events;

namespace Log2ui;

public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var observableSink = new ObservableSink();
        Log.Logger = new LoggerConfiguration()
                     .MinimumLevel.Debug()
                     .WriteTo.Sink(observableSink)
                     .CreateLogger();

        Program.BuildAvaloniaApp(observableSink)
               .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(IObservable<LogEvent> observableLog)
    {
        return AppBuilder.Configure(() => new App { ObservableLog = observableLog })
                         .UsePlatformDetect()
                         .WithInterFont()
                         .LogToSerilog()
                         .UseReactiveUI();
    }
}
