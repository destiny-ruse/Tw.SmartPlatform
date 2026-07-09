using AwesomeAssertions;
using Tw.DistributedLocking;
using Xunit;

namespace Tw.DistributedLocking.Tests;

/// <summary>验证 DistributedLockKeyBuilderTests 相关行为</summary>
public sealed class DistributedLockKeyBuilderTests
{
    /// <summary>验证 Build_IncludesTenantShardResourceAndIdentifier 场景</summary>
    [Fact]
    public void Build_IncludesTenantShardResourceAndIdentifier()
    {
        var key = DistributedLockKeyBuilder.Build("tenant-a", "shard-01", "Invoice", "inv-100");

        key.Value.Should().Be("lock:tenant-a:shard-01:Invoice:inv-100");
    }
}
