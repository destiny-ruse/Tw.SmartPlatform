using Company.Service.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Company.Service.HttpApi;

/// <summary>表示 OrdersEndpoint 类型</summary>
public static class OrdersEndpoint
{
    /// <summary>执行 MapOrders 操作</summary>
    /// <param name="endpoints">endpoints 参数</param>
    /// <returns>MapOrders 的执行结果</returns>
    public static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/orders/{id:long}", (long id) => new OrderAppService().Get(id));
        return endpoints;
    }
}
