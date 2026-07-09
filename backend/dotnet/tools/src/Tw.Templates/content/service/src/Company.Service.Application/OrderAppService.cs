using Company.Service.Application.Contracts;

namespace Company.Service.Application;

public sealed class OrderAppService
{
    public OrderDto Get(long id)
    {
        return new OrderDto(id.ToString(System.Globalization.CultureInfo.InvariantCulture), "ORD-001");
    }
}
