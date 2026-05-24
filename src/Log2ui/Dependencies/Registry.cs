using System;
using System.Linq;
using System.Reflection;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Dependencies;

public readonly struct Registry(IContainer container, IServiceCollection collection)
{
    public IContainer Container { get; } = container;
    public IServiceCollection Collection { get; } = collection;

    public static IContainer Register()
    {
        var container = new Container();
        var iocFactory = new DryIocServiceProviderFactory(container);
        var collection = new ServiceCollection();

        var me = new Registry(container, collection);
        Registry.RegisterSelfRegisters(me);

        var result = iocFactory.CreateBuilder(collection);
        foreach (var serviceDescriptor in collection.Where(a => a.Lifetime == ServiceLifetime.Singleton))
        {
            // Instanciate singletons so they dont get recreated in a child container
            result.Resolve(serviceDescriptor.ServiceType);
        }

        return result;
    }

    private static void RegisterSelfRegisters(Registry me)
    {
        var selfRegisters = typeof(App).Assembly.GetTypes()
                                       .Where(a => a.IsAssignableTo(typeof(ISelfRegistering)) && a != typeof(ISelfRegistering))
                                       .ToArray();
        var explicitName = $"{typeof(ISelfRegistering).Namespace}.{nameof(ISelfRegistering)}.{nameof(ISelfRegistering.RegisterServices)}";
        const string implicitName = nameof(ISelfRegistering.RegisterServices);
        Type[] parameterTypes = [typeof(Registry)];
        object?[] parameters = [me];
        foreach (var selfRegister in selfRegisters)
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            var method = selfRegister.GetMethod(explicitName, flags, parameterTypes)
                         ?? selfRegister.GetMethod(implicitName, flags, parameterTypes)
                         ?? throw new InvalidOperationException($"{nameof(ISelfRegistering)} without implementing {nameof(ISelfRegistering.RegisterServices)}");

            method.Invoke(null, parameters);
        }
    }
}

public interface ISelfRegistering
{
    abstract static void RegisterServices(Registry registry);
}
