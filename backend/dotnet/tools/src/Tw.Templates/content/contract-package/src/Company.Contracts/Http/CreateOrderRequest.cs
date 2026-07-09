namespace Company.Contracts.Http;

/// <summary>表示 CreateOrderRequest 声明</summary>
public sealed record CreateOrderRequest(string CustomerId, string OrderNumber);
