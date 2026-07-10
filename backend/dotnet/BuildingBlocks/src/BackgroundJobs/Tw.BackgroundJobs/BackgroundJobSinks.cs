namespace Tw.BackgroundJobs;

/// <summary>
/// 定义后台作业审计Sink的能力边界
/// </summary>
public interface IBackgroundJobAuditSink
{
    /// <summary>
    /// 记录后台作业管道的执行步骤
    /// </summary>
    /// <param name="auditEvent">用于提供auditEvent</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task RecordAsync(BackgroundJobAuditEvent auditEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// 定义后台作业TraceSink的能力边界
/// </summary>
public interface IBackgroundJobTraceSink
{
    /// <summary>
    /// 记录后台作业管道的执行步骤
    /// </summary>
    /// <param name="traceEvent">用于提供traceEvent</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task RecordAsync(BackgroundJobTraceEvent traceEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// 定义后台作业MetricSink的能力边界
/// </summary>
public interface IBackgroundJobMetricSink
{
    /// <summary>
    /// 记录后台作业管道的执行步骤
    /// </summary>
    /// <param name="metricEvent">用于提供metricEvent</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task RecordAsync(BackgroundJobMetricEvent metricEvent, CancellationToken cancellationToken = default);
}
