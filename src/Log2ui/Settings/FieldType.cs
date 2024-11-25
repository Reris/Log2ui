using System;
using System.ComponentModel;
using Log2ui.Data;

namespace Log2ui.Settings;

[Serializable]
public class FieldType
{
    public FieldType(LogMessageField field, string name, string? property = null)
    {
        this.Field = field;
        this.Name = name;
        this.Property = property;
    }

    /// <summary>
    /// Gets or sets the type of field.
    /// </summary>
    /// <value>
    /// The field.
    /// </value>
    [Category("Field Configuration")]
    [DisplayName("Field Type")]
    [Description("The Type of the Field")]
    public LogMessageField Field { get; set; }

    /// <summary>
    /// If the Field is of type Property, specify the name of the Property
    /// </summary>
    /// <value>
    /// The property.
    /// </value>
    [Category("Field Configuration")]
    [DisplayName("Property")]
    [Description("The Name of the Property")]
    public string? Property { get; set; }

    /// <summary>
    /// The Display / Column name of the Field
    /// </summary>
    /// <value>
    /// The name of the field.
    /// </value>
    [Category("Field Configuration")]
    [DisplayName("Name")]
    [Description("The Name of the Column")]
    public string Name { get; set; }

    public override string ToString() => $"{this.Name}, {this.Property}";
}
