using Serilog;
using Tw.Security.DataMasking;

namespace Tw.Observability.Serilog;

/// <summary>表示 SerilogBuilderExtensions 类型</summary>
public static class SerilogBuilderExtensions
{
    /// <summary>执行 EnrichWithTwRedaction 操作</summary>
    /// <param name="configuration">configuration 参数</param>
    /// <param name="dataMasker">dataMasker 参数</param>
    /// <returns>EnrichWithTwRedaction 的执行结果</returns>
    public static LoggerConfiguration EnrichWithTwRedaction(this LoggerConfiguration configuration, IDataMasker dataMasker)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(dataMasker);
        return configuration.Enrich.With(new RedactingLogEventEnricher(dataMasker));
    }
}
