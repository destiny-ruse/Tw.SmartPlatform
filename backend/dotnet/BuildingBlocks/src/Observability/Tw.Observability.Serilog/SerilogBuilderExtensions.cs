using Serilog;
using Tw.Security.DataMasking;

namespace Tw.Observability.Serilog;

public static class SerilogBuilderExtensions
{
    public static LoggerConfiguration EnrichWithTwRedaction(this LoggerConfiguration configuration, IDataMasker dataMasker)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(dataMasker);
        return configuration.Enrich.With(new RedactingLogEventEnricher(dataMasker));
    }
}
