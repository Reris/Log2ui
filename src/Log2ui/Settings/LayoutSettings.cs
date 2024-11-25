using System;
using System.Drawing;
using Avalonia;
using Avalonia.Controls;

namespace Log2ui.Settings
{
  [Serializable]
  public sealed class LayoutSettings
  {
    public Rectangle WindowPosition { get; set; }
    public WindowState WindowState { get; set; }
    public bool ShowLogDetailView { get; set; }
    public Rect LogDetailViewSize { get; set; }
    public bool ShowLoggerTree { get; set; }
    public Rect LoggerTreeSize { get; set; }
    public int[] LogListViewColumnsWidths { get; set; }

    public void Set(Rectangle position, WindowState state, Control detailView, Control loggerTree)
    {
      this.WindowPosition = position;
      this.WindowState = state;
      this.ShowLogDetailView = detailView.IsVisible;
      this.LogDetailViewSize = detailView.Bounds;
      this.ShowLoggerTree = loggerTree.IsVisible;
      this.LoggerTreeSize = loggerTree.Bounds;
    }
  }
}
