using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Settings;

[AttributeUsage(AttributeTargets.Class)]
public class ReceiverSettingsDiscriminatorAttribute(string typeKey, int version) : Attribute
{
    public string TypeKey => typeKey;
    public int Version => version;

    public static void Register<T>(IServiceCollection collection)
        where T : ReceiverSettings, new()
    {
        var t = typeof(T);
        var attribute = t.GetCustomAttribute<ReceiverSettingsDiscriminatorAttribute>() ?? throw new NotDeclaredException();
        var register = new ReceiverSettingsDiscriminator(t, $"{attribute.TypeKey}V{attribute.Version}");
        if (collection.Select(a => a.ImplementationInstance).OfType<ReceiverSettingsDiscriminator>().Any(a => a.TypeKey == register.TypeKey))
        {
            throw new AlreadyRegisteredException(register.TypeKey);
        }

        collection.AddSingleton(register);
        collection.AddSingleton<ReceiverSettings>(new T());
    }

    public class NotDeclaredException : Exception;

    public class AlreadyRegisteredException(string typeKey) : Exception($"'{typeKey}' is already registered.");
}
