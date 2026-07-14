using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System.Runtime.CompilerServices;

namespace Tw.AspNetCore.Health;

/// <summary>
/// 提供标准健康检查端点的路由映射入口
/// </summary>
public static class HealthEndpointRouteBuilderExtensions
{
    /// <summary>
    /// 按端点构建器保存弱引用映射状态，避免延长宿主生命周期
    /// </summary>
    private static readonly ConditionalWeakTable<IEndpointRouteBuilder, HealthEndpointMappingState>
        MappingStates = new();

    /// <summary>
    /// 将单一健康检查端点映射到 <c>/health</c>
    /// </summary>
    /// <param name="endpoints">用于注册 HTTP 路由的端点构建器</param>
    /// <returns>调用方传入的同一端点构建器</returns>
    /// <exception cref="ArgumentNullException"><paramref name="endpoints"/> 为 <see langword="null"/> 时抛出</exception>
    /// <remarks>
    /// 调用方必须先通过 <c>AddHealthChecks</c> 注册内置健康检查服务
    /// 同一端点构建器上的重复或并发调用只映射一次，并保留 ASP.NET Core 默认健康状态响应语义
    /// </remarks>
    public static IEndpointRouteBuilder MapHealthEndpoint(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var state = MappingStates.GetValue(
            endpoints,
            static _ => new HealthEndpointMappingState());

        lock (state)
        {
            if (state.IsMapped)
            {
                return endpoints;
            }

            endpoints.MapHealthChecks("/health");
            state.IsMapped = true;
        }

        return endpoints;
    }

    /// <summary>
    /// 记录单个端点构建器是否已完成健康检查路由映射
    /// </summary>
    private sealed class HealthEndpointMappingState
    {
        /// <summary>
        /// 当前端点构建器是否已映射 health 路由
        /// </summary>
        public bool IsMapped { get; set; }
    }
}
