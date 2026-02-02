using CsvHelper;
using CsvHelper.Configuration;
using Log2ui.Data;
using Log2ui.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using PropertyModels.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Log2ui.Exporters;

public class CsvExport(CsvExport.Settings settings) : IExport, ISelfRegistering
{
    public async Task ExportAsync(IEnumerable<LogMessage> logMessages, CancellationToken cancellationToken)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = settings.Delimiter,
            Quote = settings.QuoteChar.Length > 0 ? settings.QuoteChar[0] : '"',
            HasHeaderRecord = true,
            NewLine = Environment.NewLine,
        };


        await using var writer = new StreamWriter(settings.Path);
        await using var csv = new CsvWriter(writer, config);

        await csv.WriteRecordsAsync(logMessages, cancellationToken);
    }

    public static void RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<CsvExport>();
        registry.Collection.AddSingleton<ExportSettings>(new Settings());
    }

    public record Settings : ExportSettings
    {
        public override string DisplayName => "CSV";

        [Category("General")]
        [DisplayName("Save path")]
        [PathBrowsable(Filters = "CSV Files(*.csv)|*.csv", SaveMode = true)]
        public string Path
        {
            get;
            set => this.SetField(ref field, value);
        } = "";

        [Category("Configuration")]
        [DisplayName("Time Format")]
        [Description("Specifies the DateTime Format used to Parse the DateTime Field")]
        [DefaultValue("yyyy/MM/dd HH:mm:ss.fff")]
        public string DateTimeFormat
        {
            get;
            set => this.SetField(ref field, value);
        } = "yyyy/MM/dd HH:mm:ss.fff";

        [Category("Configuration")]
        [DisplayName("Quote Char")]
        [Description("If a field includes the delimiter, the whole field will be enclosed with a quote")]
        [DefaultValue("\"")]
        public string QuoteChar
        {
            get;
            set => this.SetField(ref field, value);
        } = "\"";

        [Category("Configuration")]
        [DisplayName("Delimiter ")]
        [Description("The character used to delimit each field")]
        [DefaultValue(",")]
        public string Delimiter
        {
            get;
            set => this.SetField(ref field, value);
        } = ",";

        public override ExportSettings DeepClone()
        {
            return this with { };
        }

        public override IExport CreateExporter(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<CsvExport>(serviceProvider, this);
        }
    }
}
