using Avalonia;
using JetBrains.Annotations;
using Tabalonia.Controls;

namespace Log2ui.Ui.Controls;

public class TabaloniaTabsControl : AvaloniaObject
{
    #region Content

    public static readonly AttachedProperty<object?> TabsEndContentProperty =
        AvaloniaProperty.RegisterAttached<TabsControl, object?>("TabsEndContent", typeof(TabsControl));

    [UsedImplicitly]
    public static void SetTabsEndContent(AvaloniaObject element, object? value)
    {
        element.SetValue(TabaloniaTabsControl.TabsEndContentProperty, value);
    }

    [UsedImplicitly]
    public static object? GetTabsEndContent(AvaloniaObject element)
    {
        return element.GetValue(TabaloniaTabsControl.TabsEndContentProperty);
    }

    #endregion
}
