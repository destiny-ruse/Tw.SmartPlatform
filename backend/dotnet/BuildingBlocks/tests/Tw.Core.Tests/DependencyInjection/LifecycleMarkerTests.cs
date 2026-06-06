using FluentAssertions;
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
    }

    [Fact]
    public void DependencyLifetime_HasThreeMembers()
    {
        Enum.GetNames<DependencyLifetime>().Should()
            .BeEquivalentTo("Transient", "Scoped", "Singleton");
    }
}
