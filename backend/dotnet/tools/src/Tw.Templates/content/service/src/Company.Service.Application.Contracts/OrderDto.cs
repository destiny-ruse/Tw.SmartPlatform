namespace Company.Service.Application.Contracts;

/// <summary>
/// 承载Order跨边界传输的数据
/// </summary>
public sealed record OrderDto(string Id, string Number);
