using Company.Service.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Company.Service.HttpApi;

/// <summary>
/// 注册Orders相关的 HTTP API 路由
/// </summary>
public static class OrdersEndpoint
{
    /// <summary>
    /// 将Orders注册到路由或映射表
    /// </summary>
    /// <param name="endpoints">用于注册 HTTP 路由的端点构建器</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    public static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/orders/{id:long}", (long id) => new OrderAppService().Get(id));
        return endpoints;
    }
}
