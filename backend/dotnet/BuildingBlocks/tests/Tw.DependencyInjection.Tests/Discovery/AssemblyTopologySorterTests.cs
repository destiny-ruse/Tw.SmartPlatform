using AwesomeAssertions;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Discovery;
using Tw.DependencyInjection.Diagnostics;
using Xunit;

namespace Tw.DependencyInjection.Tests.Discovery;

public class AssemblyTopologySorterTests
{
    private static AssemblyDescriptor Node(string name, params string[] references) =>
        new(name, references);

    [Fact]
    public void ServiceRegistrationException_DerivesFromException()
    {
        var exception = new ServiceRegistrationException("boom");

        exception.Should().BeAssignableTo<Exception>();
        exception.Message.Should().Be("boom");
    }

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
