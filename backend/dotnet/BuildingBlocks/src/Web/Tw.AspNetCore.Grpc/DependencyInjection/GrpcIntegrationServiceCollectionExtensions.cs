using Microsoft.Extensions.DependencyInjection;

namespace Tw.AspNetCore.Grpc;

/// <summary>
/// gRPC 专属集成注册入口
/// </summary>
public static class GrpcIntegrationServiceCollectionExtensions
{
    /// <summary>
    /// 注册 gRPC 服务端能力；gRPC 横切能力使用 gRPC 原生 interceptor
    /// </summary>
    /// <param name="services">服务注册集合</param>
    /// <returns>原始服务注册集合，用于链式调用</returns>
    /// <exception cref="ArgumentNullException">services 为 null 时抛出</exception>
    public static IServiceCollection AddGrpcIntegration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddGrpc();
        return services;
    }
}
