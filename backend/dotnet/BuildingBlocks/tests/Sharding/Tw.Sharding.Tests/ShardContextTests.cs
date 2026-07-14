using AwesomeAssertions;
using Tw.Sharding;
using Xunit;

namespace Tw.Sharding.Tests;

/// <summary>
/// 覆盖分片描述值语义与异步流上下文作用域
/// </summary>
public sealed class ShardContextTests
{
    /// <summary>
    /// 空分片描述使用稳定的策略和键
    /// </summary>
    [Fact]
    public void ShardDescriptorNone_UsesStableValues()
    {
        ShardDescriptor.None.Should().Be(new ShardDescriptor("none", "default"));
    }

    /// <summary>
    /// 相同策略和键的分片描述具有值相等语义
    /// </summary>
    [Fact]
    public void ShardDescriptor_UsesValueEquality()
    {
        var descriptor = new ShardDescriptor("month", "orders-2026");

        descriptor.Should().Be(new ShardDescriptor("month", "orders-2026"));
    }

    /// <summary>
    /// 未进入分片作用域时返回空分片描述
    /// </summary>
    [Fact]
    public void Current_DefaultsToNone()
    {
        var context = new ShardContext();

        context.Current.Should().Be(ShardDescriptor.None);
    }

    /// <summary>
    /// 空分片描述不能用于创建作用域
    /// </summary>
    [Fact]
    public void Change_RejectsNullDescriptor()
    {
        var context = new ShardContext();

        var act = () => context.Change(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("descriptor");
    }

    /// <summary>
    /// 嵌套分片作用域按层级恢复先前描述
    /// </summary>
    [Fact]
    public void Change_RestoresPreviousDescriptor_AcrossNestedScopes()
    {
        var context = new ShardContext();
        var outerDescriptor = new ShardDescriptor("month", "orders-2026");
        var innerDescriptor = new ShardDescriptor("tenant", "tenant-a");

        using (context.Change(outerDescriptor))
        {
            context.Current.Should().Be(outerDescriptor);

            using (context.Change(innerDescriptor))
            {
                context.Current.Should().Be(innerDescriptor);
            }

            context.Current.Should().Be(outerDescriptor);
        }

        context.Current.Should().Be(ShardDescriptor.None);
    }

    /// <summary>
    /// 分片作用域重复释放时只恢复一次先前描述
    /// </summary>
    [Fact]
    public void ChangeScope_Dispose_IsIdempotent()
    {
        var context = new ShardContext();
        var scope = context.Change(new ShardDescriptor("month", "orders-2026"));

        scope.Dispose();
        scope.Dispose();

        context.Current.Should().Be(ShardDescriptor.None);
    }
}
