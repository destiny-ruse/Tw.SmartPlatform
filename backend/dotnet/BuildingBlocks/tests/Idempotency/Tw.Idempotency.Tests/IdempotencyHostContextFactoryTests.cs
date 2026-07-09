using AwesomeAssertions;
using Tw.Idempotency;
using Tw.Idempotency.Hosts;
using Xunit;

namespace Tw.Idempotency.Tests;

public sealed class IdempotencyHostContextFactoryTests
{
    [Fact]
    public void HttpFactory_BuildsTenantScopedRequestKey()
    {
        var key = HttpIdempotencyContextFactory.Create("tenant-a", "Order", "Create", "request-1");

        key.Should().Be(new IdempotencyKey(IdempotencyBoundary.Http, "tenant-a", "Order", "Create", "request-1"));
    }

    [Fact]
    public void CapFactory_BuildsMessageDedupeKey()
    {
        var key = CapIdempotencyContextFactory.Create("tenant-a", "OrderCreated", "cap-message-1");

        key.Should().Be(new IdempotencyKey(IdempotencyBoundary.Cap, "tenant-a", "OrderCreated", "Consume", "cap-message-1"));
    }
}
