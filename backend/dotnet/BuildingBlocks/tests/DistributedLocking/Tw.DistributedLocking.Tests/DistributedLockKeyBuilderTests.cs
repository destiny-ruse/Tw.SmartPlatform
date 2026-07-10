using AwesomeAssertions;
using Tw.DistributedLocking;
using Xunit;

namespace Tw.DistributedLocking.Tests;

/// <summary>
/// 覆盖DistributedLock键构建器的核心行为和边界条件
/// </summary>
public sealed class DistributedLockKeyBuilderTests
{
    /// <summary>
    /// 验证BuildIncludes租户Shard资源和标识符
    /// </summary>
    [Fact]
    public void Build_IncludesTenantShardResourceAndIdentifier()
    {
        var key = DistributedLockKeyBuilder.Build("tenant-a", "shard-01", "Invoice", "inv-100");

        key.Value.Should().Be("lock:tenant-a:shard-01:Invoice:inv-100");
    }
}
