using System;
using System.ComponentModel;
using Log2ui.Data;

namespace Log2ui.Settings;

[Serializable]
public record FieldMapping(LogMessageField Field, string Name, string? Property = null)
{
    public FieldMapping()
        : this(LogMessageField.SequenceNr, "")
    {
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
    public LogMessageField Field { get; set; } = Field;

    /// <summary>
    /// If the Field is of type Property, specify the name of the Property
    /// </summary>
    /// <value>
    /// The property.
    /// </value>
    [Category("Field Configuration")]
    [DisplayName("Property")]
    [Description("The Name of the Property")]
    public string? Property { get; set; } = Property;

    /// <summary>
    /// The Display / Column name of the Field
    /// </summary>
    /// <value>
    /// The name of the field.
    /// </value>
    [Category("Field Configuration")]
    [DisplayName("Name")]
    [Description("The Name of the Column")]
    public string Name { get; set; } = Name;
}
