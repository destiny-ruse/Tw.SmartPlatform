using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Configuration.Abstractions;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

public class DependencyLifetimeResolverTests
{
    private sealed class ScopedService : IScopedDependency;
    private sealed class MultiLifetimeService : IScopedDependency, ISingletonDependency;

    [ServiceRegistration(DependencyLifetime.Singleton)]
    private sealed class AttributeLifetimeService : IScopedDependency;

    [ServiceRegistration]
    private sealed class NoLifetimeService;

    private sealed class CacheOptions : IConfigurableOptions;
    private abstract class AbstractService : IScopedDependency;

    [Fact]
    public void ResolveLifetime_UsesMarkerInterface()
    {
        ServiceTypeInspector.TryResolveLifetime(typeof(ScopedService), out var lifetime, out var reason)
            .Should().BeTrue();
        lifetime.Should().Be(DependencyLifetime.Scoped);
        reason.Should().BeNull();
    }

    [Fact]
    public void ResolveLifetime_AttributeOverridesMarker()
    {
        ServiceTypeInspector.TryResolveLifetime(typeof(AttributeLifetimeService), out var lifetime, out _)
            .Should().BeTrue();
        lifetime.Should().Be(DependencyLifetime.Singleton);
    }

    [Fact]
    public void ResolveLifetime_FailsWhenMultipleMarkersDeclared()
    {
        ServiceTypeInspector.TryResolveLifetime(typeof(MultiLifetimeService), out _, out var reason)
            .Should().BeFalse();
        reason.Should().Contain("多个生命周期标记");
    }

    [Fact]
    public void ShouldSkipOrdinaryRegistration_SkipsOptionsAndAbstractTypes()
    {
        ServiceTypeInspector.ShouldSkipOrdinaryRegistration(typeof(CacheOptions), out var optionsReason)
            .Should().BeTrue();
        optionsReason.Should().Contain("Options");

        ServiceTypeInspector.ShouldSkipOrdinaryRegistration(typeof(AbstractService), out var abstractReason)
            .Should().BeTrue();
        abstractReason.Should().Contain("抽象");
    }

    [Fact]
    public void ResolveLifetime_SkipsWhenNoLifetimeDeclared()
    {
        ServiceTypeInspector.TryResolveLifetime(typeof(NoLifetimeService), out _, out var reason)
            .Should().BeFalse();
        reason.Should().Contain("未声明生命周期");
    }

    [Theory]
    [InlineData(DependencyLifetime.Transient, ServiceLifetime.Transient)]
    [InlineData(DependencyLifetime.Scoped, ServiceLifetime.Scoped)]
    [InlineData(DependencyLifetime.Singleton, ServiceLifetime.Singleton)]
    public void Mapper_MapsToMicrosoftServiceLifetime(DependencyLifetime source, ServiceLifetime expected)
    {
        DependencyLifetimeMapper.Map(source).Should().Be(expected);
    }
}
