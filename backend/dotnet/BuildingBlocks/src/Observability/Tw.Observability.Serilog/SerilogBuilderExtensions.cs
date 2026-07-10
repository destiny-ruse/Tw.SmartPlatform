using Serilog;
using Tw.Security.DataMasking;

namespace Tw.Observability.Serilog;

/// <summary>
/// 封装Serilog构建器Extensions相关的数据和行为
/// </summary>
public static class SerilogBuilderExtensions
{
    /// <summary>
    /// 说明EnrichWithTwRedaction在当前类型中的职责
    /// </summary>
    /// <param name="configuration">用于提供configuration</param>
    /// <param name="dataMasker">用于提供dataMasker</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    public static LoggerConfiguration EnrichWithTwRedaction(this LoggerConfiguration configuration, IDataMasker dataMasker)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(dataMasker);
        return configuration.Enrich.With(new RedactingLogEventEnricher(dataMasker));
    }
}
