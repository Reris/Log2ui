using System;
using System.Collections.Generic;
using System.Reflection;

namespace Log2ui.Receivers;

public class ReceiverFactory
{
    private static ReceiverFactory? _instance;
    private static readonly string? ReceiverInterfaceName = typeof(IReceiver).FullName;

    private ReceiverFactory()
    {
        // Get all the possible receivers by enumerating all the types implementing the interface
        var assembly = Assembly.GetAssembly(typeof(IReceiver));
        var types = assembly.GetTypes();
        foreach (var type in types)
        {
            // Skip abstract types
            if (type.IsAbstract)
            {
                continue;
            }

            var findInterfaces = type.FindInterfaces((typeObj, o) => typeObj.ToString() == ReceiverFactory.ReceiverInterfaceName, null);
            if (findInterfaces.Length < 1)
            {
                continue;
            }

            this.AddReceiver(type);
        }
    }

    public static ReceiverFactory Instance => ReceiverFactory._instance ?? (ReceiverFactory._instance = new ReceiverFactory());

    public Dictionary<string, ReceiverInfo> ReceiverTypes { get; } = new();

    private void AddReceiver(Type type)
    {
        var info = new ReceiverInfo(ReceiverUtils.GetTypeDescription(type), type);
        this.ReceiverTypes.Add(type.FullName, info);
    }

    public IReceiver Create(string typeStr)
    {
        IReceiver? receiver = null;
        if (this.ReceiverTypes.TryGetValue(typeStr, out var info))
        {
            receiver = Activator.CreateInstance(info.Type) as IReceiver;
        }

        return receiver;
    }

    public record ReceiverInfo(string Name, Type Type)
    {
        public override string ToString() => this.Name;
    }
}
