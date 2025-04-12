using System;
using System.ComponentModel;
using Log2ui.Data;

namespace Log2ui.Settings;

[Serializable]
public class FieldType(LogMessageField field, string name, string? property = null)
{
    /// <summary>
    /// Gets or sets the type of field.
    /// </summary>
    /// <value>
    /// The field.
    /// </value>
    [Category("Field Configuration")]
    [DisplayName("Field Type")]
    [Description("The Type of the Field")]
    public LogMessageField Field { get; set; } = field;

    /// <summary>
    /// If the Field is of type Property, specify the name of the Property
    /// </summary>
    /// <value>
    /// The property.
    /// </value>
    [Category("Field Configuration")]
    [DisplayName("Property")]
    [Description("The Name of the Property")]
    public string? Property { get; set; } = property;

    /// <summary>
    /// The Display / Column name of the Field
    /// </summary>
    /// <value>
    /// The name of the field.
    /// </value>
    [Category("Field Configuration")]
    [DisplayName("Name")]
    [Description("The Name of the Column")]
    public string Name { get; set; } = name;

    public override string ToString()
    {
        return $"{this.Name}, {this.Property}";
    }
}
