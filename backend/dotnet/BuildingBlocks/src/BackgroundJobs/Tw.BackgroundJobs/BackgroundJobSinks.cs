namespace Tw.BackgroundJobs;

/// <summary>定义 IBackgroundJobAuditSink 契约</summary>
public interface IBackgroundJobAuditSink
{
    /// <summary>执行 RecordAsync 操作</summary>
    /// <param name="auditEvent">auditEvent 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>RecordAsync 的执行结果</returns>
    Task RecordAsync(BackgroundJobAuditEvent auditEvent, CancellationToken cancellationToken = default);
}

/// <summary>定义 IBackgroundJobTraceSink 契约</summary>
public interface IBackgroundJobTraceSink
{
    /// <summary>执行 RecordAsync 操作</summary>
    /// <param name="traceEvent">traceEvent 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>RecordAsync 的执行结果</returns>
    Task RecordAsync(BackgroundJobTraceEvent traceEvent, CancellationToken cancellationToken = default);
}

/// <summary>定义 IBackgroundJobMetricSink 契约</summary>
public interface IBackgroundJobMetricSink
{
    /// <summary>执行 RecordAsync 操作</summary>
    /// <param name="metricEvent">metricEvent 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>RecordAsync 的执行结果</returns>
    Task RecordAsync(BackgroundJobMetricEvent metricEvent, CancellationToken cancellationToken = default);
}
