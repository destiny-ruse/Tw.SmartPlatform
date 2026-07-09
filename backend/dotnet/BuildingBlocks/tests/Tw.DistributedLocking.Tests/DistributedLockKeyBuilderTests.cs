using AwesomeAssertions;
using Tw.DistributedLocking;
using Xunit;

namespace Tw.DistributedLocking.Tests;

public sealed class DistributedLockKeyBuilderTests
{
    [Fact]
    public void Build_IncludesTenantShardResourceAndIdentifier()
    {
        var key = DistributedLockKeyBuilder.Build("tenant-a", "shard-01", "Invoice", "inv-100");

        key.Value.Should().Be("lock:tenant-a:shard-01:Invoice:inv-100");
    }
}
