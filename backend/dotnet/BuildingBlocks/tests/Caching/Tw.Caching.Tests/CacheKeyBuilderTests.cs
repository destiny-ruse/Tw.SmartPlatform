using AwesomeAssertions;
using Tw.Caching;
using Xunit;

namespace Tw.Caching.Tests;

/// <summary>验证 CacheKeyBuilderTests 相关行为</summary>
public sealed class CacheKeyBuilderTests
{
    /// <summary>验证 Build_IncludesTenantShardResourceAndVersion 场景</summary>
    [Fact]
    public void Build_IncludesTenantShardResourceAndVersion()
    {
        var key = CacheKeyBuilder.Build("tenant-a", "orders-2026", "Order", "42", "v3");

        key.Value.Should().Be("tenant-a:orders-2026:Order:42:v3");
    }
}
