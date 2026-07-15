using System.Runtime.CompilerServices;
using Serilog;
using Tw.Security.DataMasking;

namespace Tw.Observability.Serilog;

/// <summary>
/// 提供Serilog敏感属性脱敏配置入口
/// </summary>
public static class SerilogBuilderExtensions
{
    /// <summary>
    /// 按配置实例保存脱敏器注册状态且不阻止配置被回收
    /// </summary>
    private static readonly ConditionalWeakTable<LoggerConfiguration, RedactionRegistrationState> RegistrationStates = new();

    /// <summary>
    /// 为日志管道注册使用指定数据脱敏器的结构化属性脱敏器
    /// </summary>
    /// <param name="configuration">需要追加脱敏器的Serilog配置</param>
    /// <param name="dataMasker">替换敏感标量属性值的数据脱敏器</param>
    /// <returns>原Serilog配置</returns>
    /// <remarks>同一配置仅首个成功注册的数据脱敏器生效，后续重复或并发调用保持幂等</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> 或 <paramref name="dataMasker"/> 为 <see langword="null"/> 时抛出</exception>
    public static LoggerConfiguration EnrichWithSensitiveDataRedaction(
        this LoggerConfiguration configuration,
        IDataMasker dataMasker)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(dataMasker);

        var state = RegistrationStates.GetValue(
            configuration,
            static _ => new RedactionRegistrationState());

        lock (state)
        {
            if (state.IsRegistered)
            {
                return configuration;
            }

            configuration.Enrich.With(new RedactingLogEventEnricher(dataMasker));
            state.IsRegistered = true;
        }

        return configuration;
    }

    /// <summary>
    /// 保存单个Serilog配置的脱敏器注册状态
    /// </summary>
    private sealed class RedactionRegistrationState
    {
        /// <summary>
        /// 获取或设置是否已成功注册脱敏器
        /// </summary>
        public bool IsRegistered { get; set; }
    }
}
