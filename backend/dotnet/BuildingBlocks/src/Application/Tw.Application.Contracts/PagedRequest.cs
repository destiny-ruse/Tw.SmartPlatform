namespace Tw.Application.Contracts;

/// <summary>
/// 分页查询请求
/// </summary>
/// <param name="PageNumber">页码，从 1 开始</param>
/// <param name="PageSize">每页记录数</param>
public sealed record PagedRequest(int PageNumber, int PageSize);
