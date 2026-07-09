using AwesomeAssertions;
using Tw.Observability;
using Xunit;

namespace Tw.Observability.Tests;

/// <summary>验证 MetricTagsTests 相关行为</summary>
public sealed class MetricTagsTests
{
    /// <summary>验证 Create_IncludesServiceTenantShardAndOperation 场景</summary>
    [Fact]
    public void Create_IncludesServiceTenantShardAndOperation()
    {
        var tags = MetricTags.Create("billing-api", "tenant-a", "shard-01", "CreateOrder");

        tags.Values.Should().ContainKey("service.name");
        tags.Values.Should().ContainKey("tenant.id");
        tags.Values.Should().ContainKey("shard.id");
        tags.Values.Should().ContainKey("operation.name");
    }
}
