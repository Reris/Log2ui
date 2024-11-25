using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Log2ui.Ui.Controls;

[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
public class ContentThumb : Thumb
{
    #region Content

    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<ContentThumb, object?>(nameof(ContentThumb.Content));

    [Content]
    [DependsOn(nameof(ContentThumb.ContentTemplate))]
    public object? Content
    {
        get => this.GetValue(ContentThumb.ContentProperty);
        set => this.SetValue(ContentThumb.ContentProperty, value);
    }

    #endregion

    #region ContentTemplate

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<ContentControl, IDataTemplate?>(nameof(ContentThumb.ContentTemplate));

    public IDataTemplate? ContentTemplate
    {
        get => this.GetValue(ContentThumb.ContentTemplateProperty);
        set => this.SetValue(ContentThumb.ContentTemplateProperty, value);
    }

    #endregion
}
