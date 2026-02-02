using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Log2ui.Data;
using Log2ui.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Exporters;

public class JsonExport : IExport, ISelfRegistering
{
    public Task ExportAsync(IEnumerable<LogMessage> logMessages, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public static void RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<JsonExport>();
        registry.Collection.AddSingleton<ExportSettings>(new Settings());
    }

    public record Settings : ExportSettings
    {
        public override string DisplayName => "JSON";

        public override ExportSettings DeepClone()
        {
            return this with { };
        }

        public override IExport CreateExporter(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<JsonExport>(serviceProvider, this);
        }
    }
}
