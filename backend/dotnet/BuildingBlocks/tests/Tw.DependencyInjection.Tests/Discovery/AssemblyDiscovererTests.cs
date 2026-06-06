using System.Reflection;
using FluentAssertions;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Diagnostics;
using Tw.DependencyInjection.Discovery;
using Xunit;

namespace Tw.DependencyInjection.Tests.Discovery;

public class AssemblyDiscovererTests
{
    private sealed class FakeAssemblySource(params Assembly[] assemblies) : IAssemblySource
    {
        public IReadOnlyList<Assembly> GetCandidateAssemblies() => assemblies;
    }

    [Fact]
    public void Report_ExposesScanAndTopologySections()
    {
        var report = new ServiceRegistrationReport(
            scannedAssemblies: ["Tw.Core"],
            excludedAssemblies: ["System.Text.Json"],
            topology: [new AssemblyTopologyEntry("Tw.Core", 0)]);

        report.ScannedAssemblies.Should().ContainSingle().Which.Should().Be("Tw.Core");
        report.ExcludedAssemblies.Should().ContainSingle().Which.Should().Be("System.Text.Json");
        report.Topology.Should().ContainSingle().Which.Level.Should().Be(0);
    }

    [Fact]
    public void Discover_FiltersToTwPrefix_AndOrdersCoreBeforeEngine()
    {
        var coreAssembly = typeof(Tw.Check).Assembly;
        var engineAssembly = typeof(ServiceRegistrationOptions).Assembly;
        var systemAssembly = typeof(string).Assembly;
        var source = new FakeAssemblySource(engineAssembly, systemAssembly, coreAssembly);

        var result = AssemblyDiscoverer.Discover(new ServiceRegistrationOptions(), source);

        result.OrderedAssemblies.Select(a => a.GetName().Name)
            .Should().ContainInOrder("Tw.Core", "Tw.DependencyInjection");
        result.Report.ScannedAssemblies.Should().Contain(["Tw.Core", "Tw.DependencyInjection"]);
        result.Report.ExcludedAssemblies.Should().Contain(systemAssembly.GetName().Name!);
    }
}
