using AwesomeAssertions;
using Tw.Caching;
using Xunit;

namespace Tw.Caching.Tests;

public sealed class CacheKeyBuilderTests
{
    [Fact]
    public void Build_IncludesTenantShardResourceAndVersion()
    {
        var key = CacheKeyBuilder.Build("tenant-a", "orders-2026", "Order", "42", "v3");

        key.Value.Should().Be("tenant-a:orders-2026:Order:42:v3");
    }
}
