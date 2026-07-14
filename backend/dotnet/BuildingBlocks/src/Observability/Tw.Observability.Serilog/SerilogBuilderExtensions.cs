using Serilog;
using Tw.Security.DataMasking;

namespace Tw.Observability.Serilog;

/// <summary>
/// 提供Serilog敏感属性脱敏配置入口
/// </summary>
public static class SerilogBuilderExtensions
{
    /// <summary>
    /// 为日志管道注册使用指定数据脱敏器的结构化属性脱敏器
    /// </summary>
    /// <param name="configuration">需要追加脱敏器的Serilog配置</param>
    /// <param name="dataMasker">替换敏感标量属性值的数据脱敏器</param>
    /// <returns>已追加敏感属性脱敏器的原Serilog配置</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> 或 <paramref name="dataMasker"/> 为 <see langword="null"/> 时抛出</exception>
    public static LoggerConfiguration EnrichWithSensitiveDataRedaction(
        this LoggerConfiguration configuration,
        IDataMasker dataMasker)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(dataMasker);
        return configuration.Enrich.With(new RedactingLogEventEnricher(dataMasker));
    }
}
