using Riok.Mapperly.Abstractions;

namespace Log2ui.Settings.Services;

[Mapper]
public partial class Mapper
{
    public static NamedLoggerSettings DefaultToNamed(LoggerSettings settings, string name)
    {
        return Mapper.DefaultToNamed(settings, name, name);
    }

    public static partial NamedLoggerSettings DefaultToNamed(LoggerSettings settings, string name, string originalName);
}
