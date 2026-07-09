namespace Company.Contracts.Http;

public sealed record CreateOrderRequest(string CustomerId, string OrderNumber);
