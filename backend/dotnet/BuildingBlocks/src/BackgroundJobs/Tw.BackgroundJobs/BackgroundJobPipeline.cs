using MediatR;

namespace Tw.BackgroundJobs;

/// <summary>
/// 封装后台作业管道相关的数据和行为
/// </summary>
public sealed class BackgroundJobPipeline(
    ISender sender,
    IBackgroundJobAuditSink auditSink,
    IBackgroundJobTraceSink traceSink,
    IBackgroundJobMetricSink metricSink)
{
    /// <summary>
    /// 异步执行当前组件的核心处理流程
    /// </summary>
    /// <param name="command">用于提供command</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
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
