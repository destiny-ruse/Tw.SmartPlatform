namespace Tw.Application.Contracts;

/// <summary>
/// 分页查询结果
/// </summary>
/// <typeparam name="T">结果项类型</typeparam>
/// <param name="Items">当前页结果项</param>
/// <param name="TotalCount">满足查询条件的总记录数</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, long TotalCount);
