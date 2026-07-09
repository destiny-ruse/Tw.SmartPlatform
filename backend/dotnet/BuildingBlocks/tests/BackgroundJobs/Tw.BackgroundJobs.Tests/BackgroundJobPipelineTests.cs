using AwesomeAssertions;
using MediatR;
using NSubstitute;
using Tw.BackgroundJobs;
using Tw.BackgroundJobs.Abstractions;
using Xunit;

namespace Tw.BackgroundJobs.Tests;

/// <summary>验证 BackgroundJobPipelineTests 相关行为</summary>
public sealed class BackgroundJobPipelineTests
{
    /// <summary>验证 ExecuteAsync_SendsCommandAndRecordsAuditTraceAndMetrics 场景</summary>
    /// <returns>ExecuteAsync_SendsCommandAndRecordsAuditTraceAndMetrics 的执行结果</returns>
    [Fact]
    public async Task ExecuteAsync_SendsCommandAndRecordsAuditTraceAndMetrics()
    {
        var sender = Substitute.For<ISender>();
        var auditSink = new RecordingJobAuditSink();
        var traceSink = new RecordingJobTraceSink();
        var metricSink = new RecordingJobMetricSink();
        var pipeline = new BackgroundJobPipeline(sender, auditSink, traceSink, metricSink);
        var context = new BackgroundJobContext("tenant-a", "default", "job-1", DateTimeOffset.UtcNow);
        var request = new SampleCommand("order-1");

        await pipeline.ExecuteAsync(new BackgroundJobCommand(request, context), TestContext.Current.CancellationToken);

        await sender.Received(1).Send(request, Arg.Any<CancellationToken>());
        auditSink.Events.Should().Contain(e => e.TenantId == "tenant-a" && e.JobId == "job-1");
        traceSink.Events.Should().Contain(e => e.JobId == "job-1" && e.EventName == "background_job.started");
        metricSink.Events.Should().Contain(e => e.JobId == "job-1" && e.MetricName == "background_job.succeeded");
    }

    /// <summary>表示 SampleCommand 声明</summary>
    private sealed record SampleCommand(string OrderId) : IRequest;

    /// <summary>验证 RecordingJobAuditSink 相关行为</summary>
    private sealed class RecordingJobAuditSink : IBackgroundJobAuditSink
    {
        /// <summary>表示 Events 属性</summary>
        public List<BackgroundJobAuditEvent> Events { get; } = [];

        /// <summary>验证 RecordAsync 场景</summary>
        /// <param name="auditEvent">auditEvent 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>RecordAsync 的执行结果</returns>
        public Task RecordAsync(BackgroundJobAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    /// <summary>验证 RecordingJobTraceSink 相关行为</summary>
    private sealed class RecordingJobTraceSink : IBackgroundJobTraceSink
    {
        /// <summary>表示 Events 属性</summary>
        public List<BackgroundJobTraceEvent> Events { get; } = [];

        /// <summary>验证 RecordAsync 场景</summary>
        /// <param name="traceEvent">traceEvent 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>RecordAsync 的执行结果</returns>
        public Task RecordAsync(BackgroundJobTraceEvent traceEvent, CancellationToken cancellationToken)
        {
            Events.Add(traceEvent);
            return Task.CompletedTask;
        }
    }

    /// <summary>验证 RecordingJobMetricSink 相关行为</summary>
    private sealed class RecordingJobMetricSink : IBackgroundJobMetricSink
    {
        /// <summary>表示 Events 属性</summary>
        public List<BackgroundJobMetricEvent> Events { get; } = [];

        /// <summary>验证 RecordAsync 场景</summary>
        /// <param name="metricEvent">metricEvent 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>RecordAsync 的执行结果</returns>
        public Task RecordAsync(BackgroundJobMetricEvent metricEvent, CancellationToken cancellationToken)
        {
            Events.Add(metricEvent);
            return Task.CompletedTask;
        }
    }
}
