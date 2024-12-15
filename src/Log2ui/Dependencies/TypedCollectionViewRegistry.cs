using System.Collections.Generic;
using JetBrains.Annotations;
using Log2ui.Collections;
using Log2ui.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Dependencies;

[UsedImplicitly]
public class TypedCollectionViewRegistry : ISelfRegistering
{
    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddScoped<ICollectionView<LogMessageItem>, TypedCollectionView<LogMessageItem>>();
        registry.Collection.AddScoped<ICollectionView<LogMessageItem, IList<LogMessageItem>>>(a => a.GetRequiredService<ICollectionView<LogMessageItem>>());
    }
}
