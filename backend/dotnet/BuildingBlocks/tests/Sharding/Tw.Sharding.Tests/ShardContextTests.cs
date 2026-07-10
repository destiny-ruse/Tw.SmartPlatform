using AwesomeAssertions;
using Tw.Sharding;
using Tw.Sharding.Abstractions;
using Xunit;

namespace Tw.Sharding.Tests;

/// <summary>
/// 覆盖Shard上下文的核心行为和边界条件
/// </summary>
public sealed class ShardContextTests
{
    /// <summary>
    /// 验证CurrentDefaults到None
    /// </summary>
    [Fact]
    public void Current_DefaultsToNone()
    {
        var context = new ShardContext();

        context.Current.Should().Be(ShardDescriptor.None);
    }

    /// <summary>
    /// 验证ChangeSetsCurrentShardInside作用域和RestoresPrevious值
    /// </summary>
    [Fact]
    public void Change_SetsCurrentShardInsideScopeAndRestoresPreviousValue()
    {
        var context = new ShardContext();

        using (context.Change(new ShardDescriptor("month", "orders-2026")))
        {
            context.Current.Should().Be(new ShardDescriptor("month", "orders-2026"));
        }

        context.Current.Should().Be(ShardDescriptor.None);
    }
}
