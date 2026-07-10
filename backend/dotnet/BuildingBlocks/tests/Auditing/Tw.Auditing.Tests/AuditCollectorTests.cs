using AwesomeAssertions;
using Tw.Auditing;
using Tw.Auditing.Contracts;
using Xunit;

namespace Tw.Auditing.Tests;

/// <summary>
/// 覆盖审计Collector的核心行为和边界条件
/// </summary>
public sealed class AuditCollectorTests
{
    /// <summary>
    /// 验证Collect异步RedactsRawSensitivePayload
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 覆盖InMemory审计存储的核心行为和边界条件
    /// </summary>
    private sealed class InMemoryAuditStore : IAuditStore
    {
        /// <summary>
        /// Events在当前对象中的业务含义
        /// </summary>
        public List<AuditEvent> Events { get; } = [];

        /// <summary>
        /// 说明存储Async在当前类型中的职责
        /// </summary>
        /// <param name="auditEvent">用于提供auditEvent</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public Task StoreAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }
}
