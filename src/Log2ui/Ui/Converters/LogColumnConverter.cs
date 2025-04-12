using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Log2ui.Data;
using Log2ui.Settings;
using Serilog;
using static Log2ui.Views.LoggerViewModel;

namespace Log2ui.Ui.Converters;

public class LogColumnConverter : IMultiValueConverter
{
    private const string MessagePart = nameof(LogMessageItem.Message) + ".";
    private static readonly ILogger Logger = Log.ForContext<LogColumnConverter>();
    private static readonly string[] PossibleProperties = typeof(LogMessage).GetProperties().Select(a => a.Name).ToArray();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] is not IReadOnlyList<LogColumn> columns)
        {
            return null;
        }

        if (values[1] is not LoggerSettingsWrapper settings)
        {
            return null;
        }

        var checkedColumns = columns.Where(LogColumnConverter.CheckPossible).ToArray();

        var results = new ObservableCollection<DataGridColumn>();
        foreach (var column in checkedColumns)
        {
            DataGridColumn result = column.Type switch
            {
                LogPropertyType.String => new DataGridTextColumn
                {
                    Header = column.Column,
                    Binding = new Binding(LogColumnConverter.MessagePart + column.Property, BindingMode.OneWay),
                },
                LogPropertyType.LoggerName => new DataGridTextColumn
                {
                    Header = column.Column,
                    Binding = new Binding(LogColumnConverter.MessagePart + column.Property, BindingMode.OneWay),
                },
                LogPropertyType.LogLevel => new DataGridTemplateColumn
                {
                    Header = column.Column,
                    CellTemplate = new FuncDataTemplate(
                        typeof(LogColumn),
                        (_, _) =>
                        {
                            var panel = new Panel
                            {
                                Classes = { nameof(LogLevel) },
                                Children =
                                {
                                    new TextBlock
                                    {
                                        [!TextBlock.TextProperty] = new Binding(LogColumnConverter.MessagePart + column.Property, BindingMode.OneWay),
                                    },
                                },
                            };
                            panel.PropertyChanged += this.LogLevelPanel_OnPropertyChanged;
                            return panel;
                        }),
                },
                LogPropertyType.DateTime => new DataGridTextColumn
                {
                    Header = column.Column,
                    Binding = new MultiBinding
                    {
                        Converter = DynamicStringFormatConverter.Instance,
                        Bindings =
                        [
                            new Binding(nameof(LoggerSettings.TimeStampFormatString) + "^", BindingMode.OneWay) { FallbackValue = "{0}", Source = settings },
                            new Binding(LogColumnConverter.MessagePart + column.Property, BindingMode.OneWay),
                        ],
                    },
                },
                LogPropertyType.Numeric => new DataGridTextColumn
                {
                    Header = column.Column,
                    Binding = new Binding(LogColumnConverter.MessagePart + column.Property, BindingMode.OneWay),
                },
                _ => throw new ArgumentOutOfRangeException(),
            };
            results.Add(result);
        }

        return results;
    }

    private void LogLevelPanel_OnPropertyChanged(object? _, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != StyledElement.DataContextProperty)
        {
            return;
        }

        LogColumnConverter.LogLevelAsClass(
            e,
            (e.OldValue as LogMessageItem)?.Message.Level,
            (e.NewValue as LogMessageItem)?.Message.Level);
    }

    public static void LogLevelAsClass(AvaloniaPropertyChangedEventArgs e, LogLevel? oldLevel, LogLevel? newLevel)
    {
        if (e.Property != StyledElement.DataContextProperty || e.Sender is not StyledElement element)
        {
            return;
        }

        if (oldLevel.HasValue)
        {
            element.Classes.Remove(oldLevel.Value.ToString());
        }

        if (newLevel.HasValue)
        {
            element.Classes.Add(newLevel.Value.ToString());
        }
    }

    private static bool CheckPossible(LogColumn a)
    {
        if (LogColumnConverter.PossibleProperties.Contains(a.Property))
        {
            return true;
        }

        const string template = $"Property {nameof(LogMessage)}.{{Property}} does not exist.";
        LogColumnConverter.Logger.Error(template, a.Property);
        return false;
    }
}
