using AwesomeAssertions;
using Tw.Sharding;
using Tw.Sharding.Abstractions;
using Xunit;

namespace Tw.Sharding.Tests;

/// <summary>验证 ShardContextTests 相关行为</summary>
public sealed class ShardContextTests
{
    /// <summary>验证 Current_DefaultsToNone 场景</summary>
    [Fact]
    public void Current_DefaultsToNone()
    {
        var context = new ShardContext();

        context.Current.Should().Be(ShardDescriptor.None);
    }

    /// <summary>验证 Change_SetsCurrentShardInsideScopeAndRestoresPreviousValue 场景</summary>
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
