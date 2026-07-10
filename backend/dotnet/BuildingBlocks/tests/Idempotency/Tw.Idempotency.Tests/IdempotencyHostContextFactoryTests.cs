using AwesomeAssertions;
using Tw.Idempotency;
using Tw.Idempotency.Hosts;
using Xunit;

namespace Tw.Idempotency.Tests;

/// <summary>
/// 覆盖幂等主机上下文工厂的核心行为和边界条件
/// </summary>
public sealed class IdempotencyHostContextFactoryTests
{
    /// <summary>
    /// 验证HttpFactoryBuilds租户Scoped请求键
    /// </summary>
    [Fact]
    public void HttpFactory_BuildsTenantScopedRequestKey()
    {
        var key = HttpIdempotencyContextFactory.Create("tenant-a", "Order", "Create", "request-1");

        key.Should().Be(new IdempotencyKey(IdempotencyBoundary.Http, "tenant-a", "Order", "Create", "request-1"));
    }

    /// <summary>
    /// 验证CapFactoryBuilds消息Dedupe键
    /// </summary>
    [Fact]
    public void CapFactory_BuildsMessageDedupeKey()
    {
        var key = CapIdempotencyContextFactory.Create("tenant-a", "OrderCreated", "cap-message-1");

        key.Should().Be(new IdempotencyKey(IdempotencyBoundary.Cap, "tenant-a", "OrderCreated", "Consume", "cap-message-1"));
    }
}
