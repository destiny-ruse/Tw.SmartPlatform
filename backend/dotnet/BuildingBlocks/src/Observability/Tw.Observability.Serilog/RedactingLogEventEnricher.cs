using Serilog.Core;
using Serilog.Events;
using Tw.Security.DataMasking;

namespace Tw.Observability.Serilog;

public sealed class RedactingLogEventEnricher(IDataMasker dataMasker) : ILogEventEnricher
{
    private static readonly string[] SensitiveNames = ["password", "secret", "token", "connectionstring"];

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var property in logEvent.Properties.ToArray())
        {
            if (!IsSensitive(property.Key) || property.Value is not ScalarValue scalar)
            {
                continue;
            }

            var masked = dataMasker.Mask(Convert.ToString(scalar.Value), SensitiveDataKind.Token);
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(property.Key, masked));
        }
    }

    private static bool IsSensitive(string name)
    {
        return SensitiveNames.Any(value => name.Contains(value, StringComparison.OrdinalIgnoreCase));
    }
}
