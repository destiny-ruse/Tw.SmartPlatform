using AwesomeAssertions;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Discovery;
using Tw.DependencyInjection.Diagnostics;
using Xunit;

namespace Tw.DependencyInjection.Tests.Discovery;

/// <summary>
/// 覆盖AssemblyTopologySorter的核心行为和边界条件
/// </summary>
public class AssemblyTopologySorterTests
{
    /// <summary>
    /// 说明Node在当前类型中的职责
    /// </summary>
    /// <param name="name">待匹配成员或资源的名称</param>
    /// <param name="references">用于提供引用</param>
    /// <returns>方法计算得到的文本值</returns>
    private static AssemblyDescriptor Node(string name, params string[] references) =>
        new(name, references);

    /// <summary>
    /// 验证服务Registration异常DerivesFrom异常
    /// </summary>
    [Fact]
    public void ServiceRegistrationException_DerivesFromException()
    {
        var exception = new ServiceRegistrationException("boom");

        exception.Should().BeAssignableTo<Exception>();
        exception.Message.Should().Be("boom");
    }

    /// <summary>
    /// 验证SortOrders依赖前置处理Dependents
    /// </summary>
    [Fact]
    public void Sort_OrdersDependenciesBeforeDependents()
    {
        var result = AssemblyTopologySorter.Sort(
        [
            Node("Tw.App", "Tw.Domain"),
            Node("Tw.Domain", "Tw.Core"),
            Node("Tw.Core"),
        ]);

        result.Select(e => e.AssemblyName).Should()
            .ContainInOrder("Tw.Core", "Tw.Domain", "Tw.App");
    }

    /// <summary>
    /// 验证SortAssignsLevelsBy依赖Depth
    /// </summary>
    [Fact]
    public void Sort_AssignsLevels_ByDependencyDepth()
    {
        var result = AssemblyTopologySorter.Sort(
        [
            Node("Tw.App", "Tw.Domain"),
            Node("Tw.Domain", "Tw.Core"),
            Node("Tw.Core"),
        ]);

        result.Should().Contain(e => e.AssemblyName == "Tw.Core" && e.Level == 0);
        result.Should().Contain(e => e.AssemblyName == "Tw.Domain" && e.Level == 1);
        result.Should().Contain(e => e.AssemblyName == "Tw.App" && e.Level == 2);
    }

    /// <summary>
    /// 验证SortIgnoresReferencesOutsideScanned写入
    /// </summary>
    [Fact]
    public void Sort_IgnoresReferences_OutsideScannedSet()
    {
        var result = AssemblyTopologySorter.Sort(
        [
            Node("Tw.Core", "System.Text.Json"),
        ]);

        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new AssemblyTopologyEntry("Tw.Core", 0));
    }

    /// <summary>
    /// 验证Sort抛出异常带有FullCycleChainOnCircular依赖
    /// </summary>
    [Fact]
    public void Sort_Throws_WithFullCycleChain_OnCircularDependency()
    {
        var act = () => AssemblyTopologySorter.Sort(
        [
            Node("Tw.A", "Tw.B"),
            Node("Tw.B", "Tw.C"),
            Node("Tw.C", "Tw.A"),
        ]);

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*Tw.A -> Tw.B -> Tw.C -> Tw.A*");
    }
}
