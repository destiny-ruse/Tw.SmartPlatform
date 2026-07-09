using Company.Service.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Company.Service.HttpApi;

public static class OrdersEndpoint
{
    public static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/orders/{id:long}", (long id) => new OrderAppService().Get(id));
        return endpoints;
    }
}
