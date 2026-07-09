using MediatR;

namespace Tw.BackgroundJobs;

/// <summary>表示 BackgroundJobPipeline 类型</summary>
public sealed class BackgroundJobPipeline(
    ISender sender,
    IBackgroundJobAuditSink auditSink,
    IBackgroundJobTraceSink traceSink,
    IBackgroundJobMetricSink metricSink)
{
    /// <summary>执行 ExecuteAsync 操作</summary>
    /// <param name="command">command 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>ExecuteAsync 的执行结果</returns>
    public async Task ExecuteAsync(BackgroundJobCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var context = command.Context;
        await traceSink.RecordAsync(new BackgroundJobTraceEvent(context.TenantId, context.ShardId, context.JobId, "background_job.started", DateTimeOffset.UtcNow), cancellationToken);

        try
        {
            await sender.Send(command.Request, cancellationToken);
            await auditSink.RecordAsync(new BackgroundJobAuditEvent(context.TenantId, context.ShardId, context.JobId, context.StartedAt), cancellationToken);
            await metricSink.RecordAsync(new BackgroundJobMetricEvent(context.TenantId, context.ShardId, context.JobId, "background_job.succeeded", 1), cancellationToken);
        }
        catch
        {
            await metricSink.RecordAsync(new BackgroundJobMetricEvent(context.TenantId, context.ShardId, context.JobId, "background_job.failed", 1), cancellationToken);
            throw;
        }
    }
}
