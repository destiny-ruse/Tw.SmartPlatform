using Company.Service.Application.Contracts;

namespace Company.Service.Application;

/// <summary>
/// 封装OrderApp服务相关的数据和行为
/// </summary>
public sealed class OrderAppService
{
    /// <summary>
    /// 说明读取在当前类型中的职责
    /// </summary>
    /// <param name="id">解析得到的长整型标识</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    public OrderDto Get(long id)
    {
        return new OrderDto(id.ToString(System.Globalization.CultureInfo.InvariantCulture), "ORD-001");
    }
}
