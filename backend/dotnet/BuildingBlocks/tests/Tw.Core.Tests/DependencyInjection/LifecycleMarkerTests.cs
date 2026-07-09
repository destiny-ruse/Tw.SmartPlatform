using AwesomeAssertions;
using Tw.DependencyInjection.Abstractions;
using Xunit;

namespace Tw.Core.Tests.DependencyInjection;

public class LifecycleMarkerTests
{
    [Fact]
    public void Markers_LiveIn_AbstractionsNamespace()
    {
        typeof(ITransientDependency).Namespace.Should().Be("Tw.DependencyInjection.Abstractions");
        typeof(IScopedDependency).Namespace.Should().Be("Tw.DependencyInjection.Abstractions");
        typeof(ISingletonDependency).Namespace.Should().Be("Tw.DependencyInjection.Abstractions");
        typeof(DependencyLifetime).Namespace.Should().Be("Tw.DependencyInjection.Abstractions");
    }

    [Fact]
    public void DependencyLifetime_HasThreeMembers_InStableOrder()
    {
        Enum.GetNames<DependencyLifetime>().Should()
            .Equal("Transient", "Scoped", "Singleton");
    }

    [Fact]
    public void DependencyLifetime_NumericValues_AreStableContract()
    {
        ((int)DependencyLifetime.Transient).Should().Be(0);
        ((int)DependencyLifetime.Scoped).Should().Be(1);
        ((int)DependencyLifetime.Singleton).Should().Be(2);
    }
}
