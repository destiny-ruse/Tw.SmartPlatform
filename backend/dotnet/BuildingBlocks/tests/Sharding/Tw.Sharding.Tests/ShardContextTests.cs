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
    /// 继承作用域的子异步流各自释放后恢复先前描述且不影响创建流
    /// </summary>
    [Fact]
    public async Task ChangeScope_DisposeInInheritedFlows_RestoresEachFlowIndependently()
    {
        var context = new ShardContext();
        var previousDescriptor = new ShardDescriptor("region", "west");
        using var previousScope = context.Change(previousDescriptor);
        var currentDescriptor = new ShardDescriptor("tenant", "tenant-a");
        var scope = context.Change(currentDescriptor);

        var firstChildDescriptor = await Task.Run(() =>
        {
            scope.Dispose();
            return context.Current;
        });
        var secondChildDescriptor = await Task.Run(() =>
        {
            scope.Dispose();
            return context.Current;
        });

        firstChildDescriptor.Should().Be(previousDescriptor);
        secondChildDescriptor.Should().Be(previousDescriptor);
        context.Current.Should().Be(currentDescriptor);

        scope.Dispose();

        context.Current.Should().Be(previousDescriptor);
    }

    /// <summary>
    /// 分片作用域重复释放时只恢复一次先前描述
    /// </summary>
    [Fact]
    public void ChangeScope_Dispose_IsIdempotent()
    {
        var context = new ShardContext();
        var retiredScope = context.Change(new ShardDescriptor("month", "orders-2026"));

        retiredScope.Dispose();
        using var activeScope = context.Change(new ShardDescriptor("tenant", "tenant-a"));
        retiredScope.Dispose();

        context.Current.Should().Be(new ShardDescriptor("tenant", "tenant-a"));
    }
}
