using System.ComponentModel;
using Log2ui.Data;

namespace Log2ui.Settings;

[TypeConverter(typeof(ExpandableObjectConverter))]
public record LogColumn(string Column, LogPropertyType Type, string Property)
{
    public LogColumn()
        : this("Column", LogPropertyType.String, "Property")
    {
    }

    [Category("Log Message Configuration")]
    [DisplayName("Property Type")]
    [Description("The type of the log property to define how Log2ui should handle it")]
    public LogPropertyType Type { get; set; } = Type;

    [Category("Log Message Configuration")]
    [DisplayName("Property")]
    [Description("The name of the property a receiver maps")]
    public string Property { get; set; } = Property;

    [Category("Log Message Configuration")]
    [DisplayName("Column")]
    [Description("The display mame of the Column")]
    public string Column { get; set; } = Column;

    public LogColumn DeepClone()
    {
        return this with { };
    }
}
