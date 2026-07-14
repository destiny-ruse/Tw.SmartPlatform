namespace Tw.AspNetCore.Correlation;

/// <summary>
/// 描述入口请求的链路追踪标识与业务关联标识
/// </summary>
/// <param name="TraceId">链路追踪系统生成的可选标识</param>
/// <param name="CorrelationId">跨请求或跨消息关联业务流程的可选标识</param>
public sealed record RequestCorrelation(string? TraceId, string? CorrelationId);
