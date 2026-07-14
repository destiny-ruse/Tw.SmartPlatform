using AwesomeAssertions;
using Tw.DistributedLocking;
using Xunit;

namespace Tw.DistributedLocking.Tests;

/// <summary>
/// 验证分布式锁键值对象的构造与相等语义
/// </summary>
public sealed class DistributedLockKeyTests
{
    /// <summary>
    /// 相同键值形成相等的锁标识，不同键值保持区分
    /// </summary>
    [Fact]
    public void Constructor_UsesValueEquality()
    {
        var first = new DistributedLockKey("lock:tenant-a:shard-01:Invoice:inv-100");
        var same = new DistributedLockKey("lock:tenant-a:shard-01:Invoice:inv-100");
        var different = new DistributedLockKey("lock:tenant-a:shard-01:Invoice:inv-101");

        first.Should().Be(same);
        first.Should().NotBe(different);
    }

    /// <summary>
    /// 空键值以精确参数名拒绝进入锁提供程序
    /// </summary>
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenValueIsNull()
    {
        var act = () => new DistributedLockKey(null!);

        var exception = act.Should().Throw<ArgumentNullException>().Which;
        exception.Should().BeOfType<ArgumentNullException>();
        exception.ParamName.Should().Be("value");
    }

    /// <summary>
    /// 空白键值不能进入锁提供程序
    /// </summary>
    /// <param name="value">需要拒绝的无效锁键值</param>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Constructor_ThrowsArgumentException_WhenValueIsBlank(string value)
    {
        var act = () => new DistributedLockKey(value);

        var exception = act.Should().Throw<ArgumentException>().Which;
        exception.Should().BeOfType<ArgumentException>();
        exception.ParamName.Should().Be("value");
    }
}
