using Avalonia;
using JetBrains.Annotations;
using Tabalonia.Controls;

namespace Log2ui.Ui.Controls;

public class TabaloniaTabsControl : AvaloniaObject
{
    #region Left Content

    public static readonly AttachedProperty<object?> TabsLeftContentProperty =
        AvaloniaProperty.RegisterAttached<TabsControl, object?>("TabsLeftContent", typeof(TabsControl));

    [UsedImplicitly]
    public static void SetTabsLeftContent(AvaloniaObject element, object? value)
    {
        element.SetValue(TabaloniaTabsControl.TabsLeftContentProperty, value);
    }

    [UsedImplicitly]
    public static object? GetTabsLeftContent(AvaloniaObject element)
    {
        return element.GetValue(TabaloniaTabsControl.TabsLeftContentProperty);
    }

    #endregion

    #region Right Content

    public static readonly AttachedProperty<object?> TabsRightContentProperty =
        AvaloniaProperty.RegisterAttached<TabsControl, object?>("TabsRightContent", typeof(TabsControl));

    [UsedImplicitly]
    public static void SetTabsRightContent(AvaloniaObject element, object? value)
    {
        element.SetValue(TabaloniaTabsControl.TabsRightContentProperty, value);
    }

    [UsedImplicitly]
    public static object? GetTabsRightContent(AvaloniaObject element)
    {
        return element.GetValue(TabaloniaTabsControl.TabsRightContentProperty);
    }

    #endregion
}
