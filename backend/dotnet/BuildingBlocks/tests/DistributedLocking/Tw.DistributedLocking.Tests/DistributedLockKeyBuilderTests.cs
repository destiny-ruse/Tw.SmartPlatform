using AwesomeAssertions;
using Tw.DistributedLocking;
using Xunit;

namespace Tw.DistributedLocking.Tests;

/// <summary>
/// 验证分布式锁键生成器的组合与输入校验语义
/// </summary>
public sealed class DistributedLockKeyBuilderTests
{
    /// <summary>
    /// 锁键包含租户、分片、资源类型与资源标识
    /// </summary>
    [Fact]
    public void Build_IncludesTenantShardResourceAndIdentifier()
    {
        var key = DistributedLockKeyBuilder.Build("tenant-a", "shard-01", "Invoice", "inv-100");

        key.Value.Should().Be("lock:tenant-a:shard-01:Invoice:inv-100");
    }

    /// <summary>
    /// 任一键段为空时以精确参数名拒绝构造锁键
    /// </summary>
    /// <param name="tenantId">租户键段</param>
    /// <param name="shardId">分片键段</param>
    /// <param name="resourceType">资源类型键段</param>
    /// <param name="identifier">资源标识键段</param>
    /// <param name="expectedParameterName">预期被拒绝的参数名</param>
    [Theory]
    [InlineData(null, "shard-01", "Invoice", "inv-100", "tenantId")]
    [InlineData("tenant-a", null, "Invoice", "inv-100", "shardId")]
    [InlineData("tenant-a", "shard-01", null, "inv-100", "resourceType")]
    [InlineData("tenant-a", "shard-01", "Invoice", null, "identifier")]
    public void Build_ThrowsArgumentNullException_WhenAnyKeySegmentIsNull(
        string? tenantId,
        string? shardId,
        string? resourceType,
        string? identifier,
        string expectedParameterName)
    {
        var act = () => DistributedLockKeyBuilder.Build(
            tenantId!,
            shardId!,
            resourceType!,
            identifier!);

        var exception = act.Should().Throw<ArgumentNullException>().Which;
        exception.Should().BeOfType<ArgumentNullException>();
        exception.ParamName.Should().Be(expectedParameterName);
    }

    /// <summary>
    /// 任一键段为空白时以精确参数名拒绝构造锁键
    /// </summary>
    /// <param name="tenantId">租户键段</param>
    /// <param name="shardId">分片键段</param>
    /// <param name="resourceType">资源类型键段</param>
    /// <param name="identifier">资源标识键段</param>
    /// <param name="expectedParameterName">预期被拒绝的参数名</param>
    [Theory]
    [InlineData("", "shard-01", "Invoice", "inv-100", "tenantId")]
    [InlineData("tenant-a", " ", "Invoice", "inv-100", "shardId")]
    [InlineData("tenant-a", "shard-01", "\t", "inv-100", "resourceType")]
    [InlineData("tenant-a", "shard-01", "Invoice", "", "identifier")]
    public void Build_ThrowsArgumentException_WhenAnyKeySegmentIsBlank(
        string tenantId,
        string shardId,
        string resourceType,
        string identifier,
        string expectedParameterName)
    {
        var act = () => DistributedLockKeyBuilder.Build(tenantId, shardId, resourceType, identifier);

        var exception = act.Should().Throw<ArgumentException>().Which;
        exception.Should().BeOfType<ArgumentException>();
        exception.ParamName.Should().Be(expectedParameterName);
    }
}
