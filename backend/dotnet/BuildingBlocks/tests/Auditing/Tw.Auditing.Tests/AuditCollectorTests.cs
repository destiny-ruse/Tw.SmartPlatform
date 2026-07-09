using AwesomeAssertions;
using Tw.Auditing;
using Tw.Auditing.Contracts;
using Xunit;

namespace Tw.Auditing.Tests;

public sealed class AuditCollectorTests
{
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

    private sealed class InMemoryAuditStore : IAuditStore
    {
        public List<AuditEvent> Events { get; } = [];

        public Task StoreAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }
}
