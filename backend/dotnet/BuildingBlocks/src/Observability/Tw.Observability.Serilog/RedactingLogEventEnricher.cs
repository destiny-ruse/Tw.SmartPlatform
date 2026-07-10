using Serilog.Core;
using Serilog.Events;
using Tw.Security.DataMasking;

namespace Tw.Observability.Serilog;

/// <summary>
/// 封装RedactingLog事件Enricher相关的数据和行为
/// </summary>
public sealed class RedactingLogEventEnricher(IDataMasker dataMasker) : ILogEventEnricher
{
    /// <summary>
    /// 保存当前类型处理流程依赖的SensitiveNames
    /// </summary>
    private static readonly string[] SensitiveNames = ["password", "secret", "token", "connectionstring"];

    /// <summary>
    /// 说明Enrich在当前类型中的职责
    /// </summary>
    /// <param name="logEvent">用于提供logEvent</param>
    /// <param name="propertyFactory">用于提供property工厂</param>
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

    /// <summary>
    /// 判断Sensitive是否满足条件
    /// </summary>
    /// <param name="name">待匹配成员或资源的名称</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    private static bool IsSensitive(string name)
    {
        return SensitiveNames.Any(value => name.Contains(value, StringComparison.OrdinalIgnoreCase));
    }
}
