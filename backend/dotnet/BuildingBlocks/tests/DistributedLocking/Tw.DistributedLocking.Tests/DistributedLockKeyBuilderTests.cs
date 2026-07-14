using AwesomeAssertions;
using Tw.DistributedLocking;
using Xunit;

namespace Tw.DistributedLocking.Tests;

/// <summary>
/// 验证分布式锁键值与公开锁契约
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
    /// 相同键值形成相等的锁标识，不同键值保持区分
    /// </summary>
    [Fact]
    public void DistributedLockKey_UsesValueEquality()
    {
        var first = new DistributedLockKey("lock:tenant-a:shard-01:Invoice:inv-100");
        var same = new DistributedLockKey("lock:tenant-a:shard-01:Invoice:inv-100");
        var different = new DistributedLockKey("lock:tenant-a:shard-01:Invoice:inv-101");

        first.Should().Be(same);
        first.Should().NotBe(different);
    }

    /// <summary>
    /// 空白键值不能进入锁提供程序
    /// </summary>
    /// <param name="value">需要拒绝的无效锁键值</param>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void DistributedLockKey_Throws_WhenValueIsBlank(string value)
    {
        var act = () => new DistributedLockKey(value);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("value");
    }

    /// <summary>
    /// 任一键段为空白时拒绝构造不完整的锁键
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
    public void Build_Throws_WhenAnyKeySegmentIsBlank(
        string tenantId,
        string shardId,
        string resourceType,
        string identifier,
        string expectedParameterName)
    {
        var act = () => DistributedLockKeyBuilder.Build(tenantId, shardId, resourceType, identifier);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(expectedParameterName);
    }

    /// <summary>
    /// 获取锁契约显式接收取消令牌并把异步句柄所有权交给调用方
    /// </summary>
    [Fact]
    public void DistributedLockContract_ExposesCancellationAndCallerOwnedHandle()
    {
        var method = typeof(IDistributedLock).GetMethod(nameof(IDistributedLock.TryAcquireAsync));

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IAsyncDisposable>));
        method.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(
            typeof(DistributedLockKey),
            typeof(TimeSpan),
            typeof(CancellationToken));
        method.GetParameters()[2].HasDefaultValue.Should().BeTrue();
        method.GetParameters()[2].DefaultValue.Should().BeNull();
    }
}
