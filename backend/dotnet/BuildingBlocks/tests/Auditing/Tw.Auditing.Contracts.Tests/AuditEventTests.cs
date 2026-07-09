using AwesomeAssertions;
using Tw.Auditing.Contracts;
using Xunit;

namespace Tw.Auditing.Contracts.Tests;

/// <summary>验证 AuditEventTests 相关行为</summary>
public sealed class AuditEventTests
{
    /// <summary>验证 CreateSecurityDenied_IncludesActorTenantActionAndStableCode 场景</summary>
    [Fact]
    public void CreateSecurityDenied_IncludesActorTenantActionAndStableCode()
    {
        var actor = new AuditActor("user-1", "tenant-a", "api");
        var auditEvent = AuditEvent.SecurityDenied(actor, "Order.Delete", "AUTH:FORBIDDEN");

        auditEvent.Actor.Should().Be(actor);
        auditEvent.Action.Name.Should().Be("Order.Delete");
        auditEvent.ErrorCode.Should().Be("AUTH:FORBIDDEN");
    }
}
