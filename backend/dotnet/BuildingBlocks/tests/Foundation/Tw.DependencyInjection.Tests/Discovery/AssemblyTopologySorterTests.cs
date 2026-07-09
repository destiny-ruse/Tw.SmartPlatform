using AwesomeAssertions;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Discovery;
using Tw.DependencyInjection.Diagnostics;
using Xunit;

namespace Tw.DependencyInjection.Tests.Discovery;

/// <summary>验证 AssemblyTopologySorterTests 相关行为</summary>
public class AssemblyTopologySorterTests
{
    /// <summary>验证 Node 场景</summary>
    /// <param name="name">name 参数</param>
    /// <param name="references">references 参数</param>
    /// <returns>Node 的执行结果</returns>
    private static AssemblyDescriptor Node(string name, params string[] references) =>
        new(name, references);

    /// <summary>验证 ServiceRegistrationException_DerivesFromException 场景</summary>
    [Fact]
    public void ServiceRegistrationException_DerivesFromException()
    {
        var exception = new ServiceRegistrationException("boom");

        exception.Should().BeAssignableTo<Exception>();
        exception.Message.Should().Be("boom");
    }

    /// <summary>验证 Sort_OrdersDependenciesBeforeDependents 场景</summary>
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

    /// <summary>验证 Sort_AssignsLevels_ByDependencyDepth 场景</summary>
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

    /// <summary>验证 Sort_IgnoresReferences_OutsideScannedSet 场景</summary>
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

    /// <summary>验证 Sort_Throws_WithFullCycleChain_OnCircularDependency 场景</summary>
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
