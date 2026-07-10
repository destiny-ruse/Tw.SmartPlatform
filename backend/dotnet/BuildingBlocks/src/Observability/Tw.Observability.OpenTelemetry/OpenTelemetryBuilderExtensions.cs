using Microsoft.Extensions.DependencyInjection;

namespace Tw.Observability.OpenTelemetry;

/// <summary>
/// 封装OpenTelemetry构建器Extensions相关的数据和行为
/// </summary>
public static class OpenTelemetryBuilderExtensions
{
    /// <summary>
    /// 注册TwOpenTelemetry所需服务
    /// </summary>
    /// <param name="services">需要注册组件依赖的服务集合</param>
    /// <param name="options">用于配置当前组件行为的选项</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    public static IServiceCollection AddTwOpenTelemetry(this IServiceCollection services, OpenTelemetryRegistrationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        _ = options ?? OpenTelemetryRegistrationOptions.Default;
        return services;
    }
}
