using System.Reflection;
using AwesomeAssertions;
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

    [Fact]
    public void Discover_ReachabilityGraph_ReflectsInScopeReferences()
    {
        // Tw.DependencyInjection 在 csproj 中直接引用 Tw.Core，
        // 两者都在扫描范围内（Tw.* 前缀），故图中应存在该可达路径。
        // System.Runtime 等框架程序集被过滤出扫描范围，不应出现在图节点中。
        var coreAssembly = typeof(Tw.Check).Assembly;
        var engineAssembly = typeof(ServiceRegistrationOptions).Assembly;
        var systemAssembly = typeof(string).Assembly;
        var source = new FakeAssemblySource(engineAssembly, systemAssembly, coreAssembly);

        var result = AssemblyDiscoverer.Discover(new ServiceRegistrationOptions(), source);
        var graph = result.ReachabilityGraph;

        // Tw.DependencyInjection → Tw.Core：直接引用，应可达
        graph.CanReach("Tw.DependencyInjection", "Tw.Core").Should().BeTrue();

        // 反方向：Tw.Core 不引用 Tw.DependencyInjection，应不可达
        graph.CanReach("Tw.Core", "Tw.DependencyInjection").Should().BeFalse();

        // 扫描范围外的程序集名不在图中，应直接返回 false
        graph.CanReach("Tw.DependencyInjection", systemAssembly.GetName().Name!).Should().BeFalse();
    }
}
