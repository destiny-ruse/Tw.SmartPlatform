using FluentAssertions;
using Tw.DependencyInjection.Invocation;
using Xunit;

namespace Tw.DependencyInjection.Tests.Invocation;

/// <summary>
/// 验证 <see cref="InvocationContext"/> 和 <see cref="InvocationFeatureCollection"/> 的行为契约。
/// </summary>
public sealed class InvocationContextTests
{
    private sealed class TestFeature { public string Tag { get; init; } = ""; }
    private sealed class MissingFeature { }

    [Fact]
    public void Items_Are_Writable()
    {
        var context = new InvocationContext(CancellationToken.None);
        context.Items["TraceId"] = "abc";
        context.Items["TraceId"].Should().Be("abc");
    }

    [Fact]
    public void GetFeature_Returns_Set_Feature()
    {
        var context = new InvocationContext(CancellationToken.None);
        var feature = new TestFeature { Tag = "foo" };
        context.Features.Set(feature);

        context.GetFeature<TestFeature>().Should().BeSameAs(feature);
    }

    [Fact]
    public void GetFeature_Returns_Null_When_Missing()
    {
        var context = new InvocationContext(CancellationToken.None);
        context.GetFeature<MissingFeature>().Should().BeNull();
    }

    [Fact]
    public void CancellationToken_Is_Captured()
    {
        using var cts = new CancellationTokenSource();
        var context = new InvocationContext(cts.Token);
        context.CancellationToken.Should().Be(cts.Token);
    }

    [Fact]
    public void Items_Use_Ordinal_Comparer()
    {
        var context = new InvocationContext(CancellationToken.None);
        context.Items["TraceId"] = "abc";
        context.Items.ContainsKey("traceid").Should().BeFalse();
    }

    [Fact]
    public void Features_Property_Returns_Same_Collection_As_GetFeature()
    {
        var features = new InvocationFeatureCollection();
        var feature = new TestFeature { Tag = "bar" };
        features.Set(feature);

        var context = new InvocationContext(CancellationToken.None, features);

        context.Features.Get<TestFeature>().Should().BeSameAs(feature);
        context.GetFeature<TestFeature>().Should().BeSameAs(feature);
    }

    [Fact]
    public void FeatureCollection_Set_Overwrites_Previous_Value()
    {
        var collection = new InvocationFeatureCollection();
        var first = new TestFeature { Tag = "first" };
        var second = new TestFeature { Tag = "second" };

        collection.Set(first);
        collection.Set(second);

        collection.Get<TestFeature>().Should().BeSameAs(second);
    }
}
