using AwesomeAssertions;
using Tw.Observability;
using Xunit;

namespace Tw.Observability.Tests;

/// <summary>
/// 覆盖MetricTags的核心行为和边界条件
/// </summary>
public sealed class MetricTagsTests
{
    /// <summary>
    /// 验证创建Includes服务租户Shard和业务委托
    /// </summary>
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
