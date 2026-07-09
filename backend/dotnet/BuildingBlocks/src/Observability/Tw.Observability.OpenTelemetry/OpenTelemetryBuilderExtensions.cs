using Microsoft.Extensions.DependencyInjection;

namespace Tw.Observability.OpenTelemetry;

/// <summary>表示 OpenTelemetryBuilderExtensions 类型</summary>
public static class OpenTelemetryBuilderExtensions
{
    /// <summary>执行 AddTwOpenTelemetry 操作</summary>
    /// <param name="services">services 参数</param>
    /// <param name="options">options 参数</param>
    /// <returns>AddTwOpenTelemetry 的执行结果</returns>
    public static IServiceCollection AddTwOpenTelemetry(this IServiceCollection services, OpenTelemetryRegistrationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        _ = options ?? OpenTelemetryRegistrationOptions.Default;
        return services;
    }
}
