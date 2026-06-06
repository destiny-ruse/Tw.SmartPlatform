using FluentAssertions;
using Tw.DependencyInjection.Diagnostics;
using Xunit;

namespace Tw.DependencyInjection.Tests.Discovery;

public class AssemblyDiscovererTests
{
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
}
