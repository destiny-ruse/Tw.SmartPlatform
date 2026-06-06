using System.Reflection;
using FluentAssertions;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

public class ServicePriorityResolverTests
{
    [ServicePriority(20)]
    [ServiceRegistration(Priority = 20)]
    private sealed class TypePriorityService;

    [ServicePriority(20)]
    [ServiceRegistration(Priority = 10)]
    private sealed class ConflictingTypePriorityService;

    [Fact]
    public void ResolveTypePriority_UsesExplicitPriority()
    {
        ServicePriorityResolver.ResolveTypePriority(typeof(TypePriorityService)).Should().Be(20);
    }

    [Fact]
    public void ResolveTypePriority_FailsWhenTwoAttributesDisagree()
    {
        var act = () => ServicePriorityResolver.ResolveTypePriority(typeof(ConflictingTypePriorityService));

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*类型优先级声明不一致*");
    }

    [Fact]
    public void ResolveAssemblyPriority_ConfigOverridesAttribute()
    {
        var options = new ServiceRegistrationOptions();
        options.AssemblyPriorities.Add(typeof(TypePriorityService).Assembly.GetName().Name!, 50);

        ServicePriorityResolver.ResolveAssemblyPriority(typeof(TypePriorityService).Assembly, options)
            .Should().Be(50);
    }

    [Fact]
    public void CalculateFinalPriority_UsesTopologyBaseAssemblyAndTypePriority()
    {
        ServicePriorityResolver.CalculateFinalPriority(topologyLevel: 2, assemblyPriority: 30, typePriority: 40)
            .Should().Be(2_000_070);
    }

    [Fact]
    public void ReachabilityGraph_DetectsTransitiveDependencyPath()
    {
        var graph = new AssemblyReachabilityGraph(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["Tw.App"] = ["Tw.Domain"],
            ["Tw.Domain"] = ["Tw.Core"],
            ["Tw.Core"] = [],
        });

        graph.CanReach("Tw.App", "Tw.Core").Should().BeTrue();
        graph.CanReach("Tw.Core", "Tw.App").Should().BeFalse();
    }
}
