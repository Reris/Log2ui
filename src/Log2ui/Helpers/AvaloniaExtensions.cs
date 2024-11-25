using System.Linq;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace Log2ui.Helpers;

public static class AvaloniaExtensions
{
    public static T? FindTemplatePart<T>(this TemplatedControl control, string name)
        where T : StyledElement
        => control.GetTemplateChildren().OfType<T>().FirstOrDefault(a => a.Name == name);
}
