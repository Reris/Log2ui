using System.ComponentModel;

namespace Log2ui.Settings;

public record MappedProperty(string SourceName, string Name)
{
    [Category("Log Message Configuration")]
    [DisplayName("Source Name")]
    [Description("The name of the property in the log message")]
    public string SourceName { get; set; } = SourceName;

    [Category("Log Message Configuration")]
    [DisplayName("Name")]
    [Description("The name inside Log2ui")]
    public string Name { get; set; } = Name;
}
