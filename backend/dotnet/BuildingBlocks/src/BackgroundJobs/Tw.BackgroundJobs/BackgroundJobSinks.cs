namespace Tw.BackgroundJobs;

public interface IBackgroundJobAuditSink
{
    Task RecordAsync(BackgroundJobAuditEvent auditEvent, CancellationToken cancellationToken = default);
}

public interface IBackgroundJobTraceSink
{
    Task RecordAsync(BackgroundJobTraceEvent traceEvent, CancellationToken cancellationToken = default);
}

public interface IBackgroundJobMetricSink
{
    Task RecordAsync(BackgroundJobMetricEvent metricEvent, CancellationToken cancellationToken = default);
}
