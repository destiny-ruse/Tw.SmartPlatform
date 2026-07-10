namespace Company.Service.Domain;

/// <summary>
/// 封装Order聚合根的领域状态
/// </summary>
public sealed record OrderAggregate(long Id, string Number);
