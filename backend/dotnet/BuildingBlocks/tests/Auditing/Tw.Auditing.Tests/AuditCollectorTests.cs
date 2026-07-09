using AwesomeAssertions;
using Tw.Auditing;
using Tw.Auditing.Contracts;
using Xunit;

namespace Tw.Auditing.Tests;

/// <summary>验证 AuditCollectorTests 相关行为</summary>
public sealed class AuditCollectorTests
{
    /// <summary>验证 CollectAsync_RedactsRawSensitivePayload 场景</summary>
    /// <returns>CollectAsync_RedactsRawSensitivePayload 的执行结果</returns>
    [Fact]
    public async Task CollectAsync_RedactsRawSensitivePayload()
    {
        var store = new InMemoryAuditStore();
        var collector = new AuditCollector(store);
        var auditEvent = AuditEvent.ConfigurationChanged(
            new AuditActor("user-1", "tenant-a", "api"),
            key: "ConnectionStrings:Default",
            oldValue: "Password=old",
            newValue: "Password=new");

        await collector.CollectAsync(auditEvent, TestContext.Current.CancellationToken);

        store.Events.Single().Details.Should().NotContain("Password=old");
        store.Events.Single().Details.Should().NotContain("Password=new");
    }

    /// <summary>验证 InMemoryAuditStore 相关行为</summary>
    private sealed class InMemoryAuditStore : IAuditStore
    {
        /// <summary>表示 Events 属性</summary>
        public List<AuditEvent> Events { get; } = [];

        /// <summary>验证 StoreAsync 场景</summary>
        /// <param name="auditEvent">auditEvent 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>StoreAsync 的执行结果</returns>
        public Task StoreAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }
}
