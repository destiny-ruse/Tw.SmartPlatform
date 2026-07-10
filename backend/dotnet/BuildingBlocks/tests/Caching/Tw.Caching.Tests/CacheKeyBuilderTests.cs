using AwesomeAssertions;
using Tw.Caching;
using Xunit;

namespace Tw.Caching.Tests;

/// <summary>
/// 覆盖缓存键构建器的核心行为和边界条件
/// </summary>
public sealed class CacheKeyBuilderTests
{
    /// <summary>
    /// 验证BuildIncludes租户Shard资源和Version
    /// </summary>
    [Fact]
    public void Build_IncludesTenantShardResourceAndVersion()
    {
        var key = CacheKeyBuilder.Build("tenant-a", "orders-2026", "Order", "42", "v3");

        key.Value.Should().Be("tenant-a:orders-2026:Order:42:v3");
    }
}
