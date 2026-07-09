using Company.Service.Application.Contracts;

namespace Company.Service.Application;

/// <summary>表示 OrderAppService 类型</summary>
public sealed class OrderAppService
{
    /// <summary>执行 Get 操作</summary>
    /// <param name="id">id 参数</param>
    /// <returns>Get 的执行结果</returns>
    public OrderDto Get(long id)
    {
        return new OrderDto(id.ToString(System.Globalization.CultureInfo.InvariantCulture), "ORD-001");
    }
}
