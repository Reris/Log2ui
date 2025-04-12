using System.ComponentModel.DataAnnotations;
using Log2ui.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Settings.Services;

public class Validator : IValidator, ISelfRegistering
{
    public static void RegisterServices(Registry registry)
    {
        registry.Collection.AddSingleton<IValidator, Validator>();
    }

    public bool IsValid(object instance)
    {
        return System.ComponentModel.DataAnnotations.Validator.TryValidateObject(instance, new ValidationContext(instance), null, true);
    }
}
