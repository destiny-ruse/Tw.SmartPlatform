using System.Reflection;
using AwesomeAssertions;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Discovery;
using Tw.DependencyInjection.Diagnostics;
using Xunit;

namespace Tw.DependencyInjection.Tests.Discovery;

/// <summary>验证 AssemblyDiscovererTests 相关行为</summary>
public class AssemblyDiscovererTests
{
    /// <summary>验证 FakeAssemblySource 相关行为</summary>
    private sealed class FakeAssemblySource(params Assembly[] assemblies) : IAssemblySource
    {
        /// <summary>验证 GetCandidateAssemblies 场景</summary>
        /// <returns>GetCandidateAssemblies 的执行结果</returns>
        public IReadOnlyList<Assembly> GetCandidateAssemblies() => assemblies;
    }

    /// <summary>验证 Report_ExposesScanAndTopologySections 场景</summary>
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

    /// <summary>验证 Discover_FiltersToTwPrefix_AndOrdersAbstractionsBeforeEngine 场景</summary>
    [Fact]
    public void Discover_FiltersToTwPrefix_AndOrdersAbstractionsBeforeEngine()
    {
        var abstractionsAssembly = typeof(DependencyLifetime).Assembly;
        var engineAssembly = typeof(ServiceRegistrationOptions).Assembly;
        var systemAssembly = typeof(string).Assembly;
        var source = new FakeAssemblySource(engineAssembly, systemAssembly, abstractionsAssembly);

        var result = AssemblyDiscoverer.Discover(new ServiceRegistrationOptions(), source);

        result.OrderedAssemblies.Select(a => a.GetName().Name)
            .Should().ContainInOrder("Tw.DependencyInjection.Abstractions", "Tw.DependencyInjection");
        result.Report.ScannedAssemblies.Should().Contain(["Tw.DependencyInjection.Abstractions", "Tw.DependencyInjection"]);
        result.Report.ExcludedAssemblies.Should().Contain(systemAssembly.GetName().Name!);
    }

    /// <summary>验证 Discover_ReachabilityGraph_ReflectsInScopeReferences 场景</summary>
    [Fact]
    public void Discover_ReachabilityGraph_ReflectsInScopeReferences()
    {
        // Tw.DependencyInjection 在 csproj 中直接引用 Tw.DependencyInjection.Abstractions，
        // 两者都在扫描范围内（Tw.* 前缀），故图中应存在该可达路径。
        // System.Runtime 等框架程序集被过滤出扫描范围，不应出现在图节点中。
        var abstractionsAssembly = typeof(DependencyLifetime).Assembly;
        var engineAssembly = typeof(ServiceRegistrationOptions).Assembly;
        var systemAssembly = typeof(string).Assembly;
        var source = new FakeAssemblySource(engineAssembly, systemAssembly, abstractionsAssembly);

        var result = AssemblyDiscoverer.Discover(new ServiceRegistrationOptions(), source);
        var graph = result.ReachabilityGraph;

        // Tw.DependencyInjection → Tw.DependencyInjection.Abstractions：直接引用，应可达
        graph.CanReach("Tw.DependencyInjection", "Tw.DependencyInjection.Abstractions").Should().BeTrue();

        // 反方向：Tw.DependencyInjection.Abstractions 不引用 Tw.DependencyInjection，应不可达
        graph.CanReach("Tw.DependencyInjection.Abstractions", "Tw.DependencyInjection").Should().BeFalse();

        // 扫描范围外的程序集名不在图中，应直接返回 false
        graph.CanReach("Tw.DependencyInjection", systemAssembly.GetName().Name!).Should().BeFalse();
    }
}
