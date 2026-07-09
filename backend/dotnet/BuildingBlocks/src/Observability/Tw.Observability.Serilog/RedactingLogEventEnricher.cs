using Serilog.Core;
using Serilog.Events;
using Tw.Security.DataMasking;

namespace Tw.Observability.Serilog;

/// <summary>表示 RedactingLogEventEnricher 类型</summary>
public sealed class RedactingLogEventEnricher(IDataMasker dataMasker) : ILogEventEnricher
{
    /// <summary>表示 SensitiveNames 字段</summary>
    private static readonly string[] SensitiveNames = ["password", "secret", "token", "connectionstring"];

    /// <summary>执行 Enrich 操作</summary>
    /// <param name="logEvent">logEvent 参数</param>
    /// <param name="propertyFactory">propertyFactory 参数</param>
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

    /// <summary>执行 IsSensitive 操作</summary>
    /// <param name="name">name 参数</param>
    /// <returns>IsSensitive 的执行结果</returns>
    private static bool IsSensitive(string name)
    {
        return SensitiveNames.Any(value => name.Contains(value, StringComparison.OrdinalIgnoreCase));
    }
}
