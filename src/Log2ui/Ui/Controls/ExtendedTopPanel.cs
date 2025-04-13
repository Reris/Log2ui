using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Log2ui.Ui.Controls;

public class ExtendedTopPanel : Panel
{
    protected Parts GetParts()
    {
        return new Parts
        {
            LeftContentControl = this.Children.Single(a => a.Name == "PART_LeftContent"),
            LeftDragWindowThumb = this.Children.Single(a => a.Name == "PART_LeftDragWindowThumb"),
            TabsControl = this.Children.Single(a => a.Name == "PART_ItemsPresenter"),
            AddTabButton = this.Children.Single(a => a.Name == "PART_AddItemButton"),
            RightDragWindowThumb = this.Children.Single(a => a.Name == "PART_RightDragWindowThumb"),
            RightContentControl = this.Children.Single(a => a.Name == "PART_RightContent"),
        };
    }


    protected override Size MeasureOverride(Size availableSize)
    {
        double height = 0;
        double width = 0;

        if (this.Children.Count == 0)
        {
            return new Size(width, height);
        }

        var availableWidth = availableSize.Width;
        var availableHeight = availableSize.Height;

        var parts = this.GetParts();

        ExtendedTopPanel.MeasureControl(parts.LeftContentControl, ref width, ref availableWidth, availableHeight);
        ExtendedTopPanel.MeasureControl(parts.LeftDragWindowThumb, ref width, ref availableWidth, availableHeight);
        ExtendedTopPanel.MeasureControl(parts.AddTabButton, ref width, ref availableWidth, availableHeight);
        ExtendedTopPanel.MeasureControl(parts.RightDragWindowThumb, ref width, ref availableWidth, availableHeight);
        ExtendedTopPanel.MeasureControl(parts.RightContentControl, ref width, ref availableWidth, availableHeight);

        parts.TabsControl.Measure(new Size(availableWidth, availableHeight));
        width += parts.TabsControl.DesiredSize.Width;
        IEnumerable<Control> affectingHeight = [parts.LeftContentControl, parts.TabsControl, parts.AddTabButton, parts.RightContentControl];
        height = affectingHeight.Max(a => a.DesiredSize.Height);

        return new Size(width, height);
    }

    protected static void MeasureControl(Control control, ref double w, ref double aW, in double h)
    {
        control.Measure(new Size(aW, h));
        w += control.DesiredSize.Width;
        aW -= control.DesiredSize.Width;
    }


    protected override Size ArrangeOverride(Size finalSize)
    {
        if (this.Children.Count == 0)
        {
            return finalSize;
        }

        var parts = this.GetParts();
        var partsWidth = new PartsWidth
        {
            LeftContentControl = parts.LeftContentControl.DesiredSize.Width,
            LeftDragWindowThumb = parts.LeftDragWindowThumb.DesiredSize.Width,
            TabsControl = parts.TabsControl.DesiredSize.Width,
            AddTabButton = parts.AddTabButton.DesiredSize.Width,
            RightDragWindowThumb = parts.RightDragWindowThumb.DesiredSize.Width,
            RightContentControl = parts.RightContentControl.DesiredSize.Width,
        };

        var tabsHeight = Math.Max(parts.TabsControl.DesiredSize.Height, finalSize.Height);

        var withoutTabsWidth = partsWidth.LeftContentControl
                               + partsWidth.LeftDragWindowThumb
                               + partsWidth.AddTabButton
                               + partsWidth.RightDragWindowThumb
                               + partsWidth.RightContentControl;
        var availableTabsWidth = finalSize.Width - withoutTabsWidth;

        //|                                      finalSize.Width                                        |
        //
        //   if (partsWidth.TabsControl < availableTabsWidth):
        //|leftContent|leftThumb|tab1    |tab2    |addTabButton|         rightThumb        |rightContent|
        //
        //   else
        //|leftContent|leftThumb|tab1|tab2|tab3|tab4|tab5|tab6|tab7|addTabButton|rightThumb|rightContent|

        if (partsWidth.TabsControl < availableTabsWidth)
        {
            this.ArrangeWhenTabsFit(parts, partsWidth, tabsHeight, finalSize.Width);
            return finalSize;
        }

        this.ArrangeWhenTabsUnfit(parts, partsWidth, tabsHeight, availableTabsWidth);
        return finalSize;
    }

    /// <summary>
    /// |leftContent|leftThumb|tab1    |tab2    |addTabButton|         rightThumb        |rightContent|
    /// </summary>
    protected virtual void ArrangeWhenTabsFit(Parts parts, PartsWidth widths, double tabsHeight, double finalWidth)
    {
        double x = 0;
        parts.LeftContentControl.Arrange(new Rect(x, 0, widths.LeftContentControl, tabsHeight));
        x += widths.LeftContentControl;
        parts.LeftDragWindowThumb.Arrange(new Rect(x, 0, widths.LeftDragWindowThumb, tabsHeight));
        x += widths.LeftDragWindowThumb;
        parts.TabsControl.Arrange(new Rect(x, 0, widths.TabsControl, tabsHeight));
        x += widths.TabsControl;

        ExtendedTopPanel.ArrangeCenterVertical(parts.AddTabButton, x, tabsHeight);
        x += widths.AddTabButton;

        var availableSpaceWidth = finalWidth
                                  - widths.LeftContentControl
                                  - widths.LeftDragWindowThumb
                                  - widths.TabsControl
                                  - widths.AddTabButton
                                  - widths.RightContentControl;

        parts.RightDragWindowThumb.Arrange(new Rect(x, 0, availableSpaceWidth, tabsHeight));
        x += availableSpaceWidth;
        parts.RightContentControl.Arrange(new Rect(x, 0, widths.RightContentControl, tabsHeight));
    }

    /// <summary>
    /// |leftContent|leftThumb|tab1|tab2|tab3|tab4|tab5|tab6|tab7|addTabButton|rightThumb|rightContent|
    /// </summary>
    protected virtual void ArrangeWhenTabsUnfit(Parts parts, PartsWidth widths, double tabsHeight, double availableTabsWidth)
    {
        double x = 0;
        parts.LeftContentControl.Arrange(new Rect(x, 0, widths.LeftContentControl, tabsHeight));
        x += widths.LeftContentControl;
        parts.LeftDragWindowThumb.Arrange(new Rect(x, 0, widths.LeftDragWindowThumb, tabsHeight));
        x += widths.LeftDragWindowThumb;

        parts.TabsControl.Arrange(new Rect(x, 0, availableTabsWidth, tabsHeight));
        x += availableTabsWidth;

        ExtendedTopPanel.ArrangeCenterVertical(parts.AddTabButton, x, tabsHeight);
        x += widths.AddTabButton;

        parts.RightDragWindowThumb.Arrange(new Rect(x, 0, widths.RightDragWindowThumb, tabsHeight));
        x += widths.RightDragWindowThumb;
        parts.RightContentControl.Arrange(new Rect(x, 0, widths.RightContentControl, tabsHeight));
    }

    private static void ArrangeCenterVertical(Layoutable control, double x, double fullHeight)
    {
        var width = control.DesiredSize.Width;
        var height = control.DesiredSize.Height;

        var y = (fullHeight - height) / 2;

        control.Arrange(new Rect(x, y, width, height));
    }

    protected readonly struct Parts
    {
        public required Control LeftContentControl { get; init; }
        public required Control LeftDragWindowThumb { get; init; }
        public required Control TabsControl { get; init; }
        public required Control AddTabButton { get; init; }
        public required Control RightDragWindowThumb { get; init; }
        public required Control RightContentControl { get; init; }
    }

    protected readonly struct PartsWidth
    {
        public required double LeftContentControl { get; init; }
        public required double LeftDragWindowThumb { get; init; }
        public required double TabsControl { get; init; }
        public required double AddTabButton { get; init; }
        public required double RightDragWindowThumb { get; init; }
        public required double RightContentControl { get; init; }
    }
}
