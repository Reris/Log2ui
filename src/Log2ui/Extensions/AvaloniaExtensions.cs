using System.Linq;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Logging;
using Log2ui.Tools;

namespace Log2ui.Extensions;

public static class AvaloniaExtensions
{
    public static T? FindTemplatePart<T>(this TemplatedControl control, string name)
        where T : StyledElement
    {
        return control.GetTemplateChildren().OfType<T>().FirstOrDefault(a => a.Name == name);
    }

    public static AppBuilder LogToSerilog(this AppBuilder builder)
    {
        Logger.Sink = new AvaloniaToSerilogSink();
        return builder;
    }
}
