using AwesomeAssertions;
using MediatR;
using NSubstitute;
using Tw.BackgroundJobs;
using Tw.BackgroundJobs.Abstractions;
using Xunit;

namespace Tw.BackgroundJobs.Tests;

public sealed class BackgroundJobPipelineTests
{
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

    private sealed record SampleCommand(string OrderId) : IRequest;

    private sealed class RecordingJobAuditSink : IBackgroundJobAuditSink
    {
        public List<BackgroundJobAuditEvent> Events { get; } = [];

        public Task RecordAsync(BackgroundJobAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingJobTraceSink : IBackgroundJobTraceSink
    {
        public List<BackgroundJobTraceEvent> Events { get; } = [];

        public Task RecordAsync(BackgroundJobTraceEvent traceEvent, CancellationToken cancellationToken)
        {
            Events.Add(traceEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingJobMetricSink : IBackgroundJobMetricSink
    {
        public List<BackgroundJobMetricEvent> Events { get; } = [];

        public Task RecordAsync(BackgroundJobMetricEvent metricEvent, CancellationToken cancellationToken)
        {
            Events.Add(metricEvent);
            return Task.CompletedTask;
        }
    }
}
