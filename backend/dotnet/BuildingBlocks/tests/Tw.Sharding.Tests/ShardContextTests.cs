using AwesomeAssertions;
using Tw.Sharding;
using Tw.Sharding.Abstractions;
using Xunit;

namespace Tw.Sharding.Tests;

public sealed class ShardContextTests
{
    [Fact]
    public void Current_DefaultsToNone()
    {
        var context = new ShardContext();

        context.Current.Should().Be(ShardDescriptor.None);
    }

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
