using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Settings;

[AttributeUsage(AttributeTargets.Class)]
public class ReceiverSettingsKeyAttribute(string typeKey, int version) : Attribute
{
    public string TypeKey => typeKey;
    public int Version => version;

    public static void Register<T>(IServiceCollection collection)
        where T : ReceiverSettings
    {
        var t = typeof(T);
        var attribute = t.GetCustomAttribute<ReceiverSettingsKeyAttribute>() ?? throw new NotDeclaredException();
        var register = new ReceiverSettingsDiscriminator(t, $"{attribute.TypeKey}V{attribute.Version}");
        collection.AddSingleton(register);
    }

    public class NotDeclaredException : Exception;
}
