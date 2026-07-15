using AwesomeAssertions;
using MediatR;
using Tw.BackgroundJobs;
using Tw.BackgroundJobs.Abstractions;
using Xunit;

namespace Tw.BackgroundJobs.Tests;

/// <summary>
/// 覆盖后台作业管道的核心行为和边界条件
/// </summary>
public sealed class BackgroundJobPipelineTests
{
    /// <summary>
    /// 验证执行异步Sends命令和Records审计Trace和Metrics
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task ExecuteAsync_SendsCommandAndRecordsAuditTraceAndMetrics()
    {
        var sender = new RecordingSender();
        var auditSink = new RecordingJobAuditSink();
        var traceSink = new RecordingJobTraceSink();
        var metricSink = new RecordingJobMetricSink();
        var pipeline = new BackgroundJobPipeline(sender, auditSink, traceSink, metricSink);
        var context = new BackgroundJobContext("tenant-a", "default", "job-1", DateTimeOffset.UtcNow);
        var request = new SampleCommand("order-1");

        await pipeline.ExecuteAsync(new BackgroundJobCommand(request, context), TestContext.Current.CancellationToken);

        sender.Requests.Should().ContainSingle().Which.Should().BeSameAs(request);
        auditSink.Events.Should().Contain(e => e.TenantId == "tenant-a" && e.JobId == "job-1");
        traceSink.Events.Should().Contain(e => e.JobId == "job-1" && e.EventName == "background_job.started");
        metricSink.Events.Should().Contain(e => e.JobId == "job-1" && e.MetricName == "background_job.succeeded");
    }

    /// <summary>
    /// 提供 CLI 中示例命令的入口描述
    /// </summary>
    private sealed record SampleCommand(string OrderId) : IRequest;

    /// <summary>
    /// 记录后台作业管道发送的请求，避免测试替身引入动态代理依赖
    /// </summary>
    private sealed class RecordingSender : ISender
    {
        /// <summary>
        /// 已发送的请求
        /// </summary>
        public List<object> Requests { get; } = [];

        /// <inheritdoc />
        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(default(TResponse)!);
        }

        /// <inheritdoc />
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult<object?>(null);
        }

        /// <inheritdoc />
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// 覆盖Recording作业审计Sink的核心行为和边界条件
    /// </summary>
    private sealed class RecordingJobAuditSink : IBackgroundJobAuditSink
    {
        /// <summary>
        /// Events在当前对象中的业务含义
        /// </summary>
        public List<BackgroundJobAuditEvent> Events { get; } = [];

        /// <summary>
        /// 记录后台作业管道的执行步骤
        /// </summary>
        /// <param name="auditEvent">用于提供auditEvent</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public Task RecordAsync(BackgroundJobAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 覆盖Recording作业TraceSink的核心行为和边界条件
    /// </summary>
    private sealed class RecordingJobTraceSink : IBackgroundJobTraceSink
    {
        /// <summary>
        /// Events在当前对象中的业务含义
        /// </summary>
        public List<BackgroundJobTraceEvent> Events { get; } = [];

        /// <summary>
        /// 记录后台作业管道的执行步骤
        /// </summary>
        /// <param name="traceEvent">用于提供traceEvent</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public Task RecordAsync(BackgroundJobTraceEvent traceEvent, CancellationToken cancellationToken)
        {
            Events.Add(traceEvent);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 覆盖Recording作业MetricSink的核心行为和边界条件
    /// </summary>
    private sealed class RecordingJobMetricSink : IBackgroundJobMetricSink
    {
        /// <summary>
        /// Events在当前对象中的业务含义
        /// </summary>
        public List<BackgroundJobMetricEvent> Events { get; } = [];

        /// <summary>
        /// 记录后台作业管道的执行步骤
        /// </summary>
        /// <param name="metricEvent">用于提供metricEvent</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public Task RecordAsync(BackgroundJobMetricEvent metricEvent, CancellationToken cancellationToken)
        {
            Events.Add(metricEvent);
            return Task.CompletedTask;
        }
    }
}
