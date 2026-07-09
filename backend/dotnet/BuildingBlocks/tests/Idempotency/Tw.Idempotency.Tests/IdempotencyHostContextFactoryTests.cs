using AwesomeAssertions;
using Tw.Idempotency;
using Tw.Idempotency.Hosts;
using Xunit;

namespace Tw.Idempotency.Tests;

/// <summary>验证 IdempotencyHostContextFactoryTests 相关行为</summary>
public sealed class IdempotencyHostContextFactoryTests
{
    /// <summary>验证 HttpFactory_BuildsTenantScopedRequestKey 场景</summary>
    [Fact]
    public void HttpFactory_BuildsTenantScopedRequestKey()
    {
        var key = HttpIdempotencyContextFactory.Create("tenant-a", "Order", "Create", "request-1");

        key.Should().Be(new IdempotencyKey(IdempotencyBoundary.Http, "tenant-a", "Order", "Create", "request-1"));
    }

    /// <summary>验证 CapFactory_BuildsMessageDedupeKey 场景</summary>
    [Fact]
    public void CapFactory_BuildsMessageDedupeKey()
    {
        var key = CapIdempotencyContextFactory.Create("tenant-a", "OrderCreated", "cap-message-1");

        key.Should().Be(new IdempotencyKey(IdempotencyBoundary.Cap, "tenant-a", "OrderCreated", "Consume", "cap-message-1"));
    }
}
