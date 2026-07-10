namespace Company.Contracts.Http;

/// <summary>
/// 封装创建Order请求相关的数据和行为
/// </summary>
public sealed record CreateOrderRequest(string CustomerId, string OrderNumber);
