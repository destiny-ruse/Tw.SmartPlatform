using FluentAssertions;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

public class ServiceExposureResolverTests
{
    private interface IOrderService;
    private interface IRepository<TEntity>;

    private sealed class OrderService : IOrderService, IScopedDependency;

    [ExposeServices(typeof(IOrderService), IncludeSelf = true)]
    private sealed class ExplicitOrderService : IOrderService, IScopedDependency;

    [ExposeKeyedService(typeof(IOrderService), "primary")]
    private sealed class KeyedOrderService : IOrderService, IScopedDependency;

    private sealed class Repository<TEntity> : IRepository<TEntity>, IScopedDependency;

    [Fact]
    public void Resolve_DefaultExposesSelfAndMatchingInterface()
    {
        var exposures = ServiceExposureResolver.Resolve(typeof(OrderService));

        exposures.Should().Contain(e => e.ServiceType == typeof(OrderService) && e.Key == null);
        exposures.Should().Contain(e => e.ServiceType == typeof(IOrderService) && e.Key == null);
        exposures.Should().NotContain(e => e.ServiceType == typeof(IScopedDependency));
    }

    [Fact]
    public void Resolve_ExplicitExposeServicesHonorsIncludeSelf()
    {
        var exposures = ServiceExposureResolver.Resolve(typeof(ExplicitOrderService));

        exposures.Should().Contain(e => e.ServiceType == typeof(IOrderService) && e.Key == null);
        exposures.Should().Contain(e => e.ServiceType == typeof(ExplicitOrderService) && e.Key == null);
    }

    [Fact]
    public void Resolve_KeyedExposureCarriesKey()
    {
        var exposures = ServiceExposureResolver.Resolve(typeof(KeyedOrderService));

        exposures.Should().ContainSingle(e => e.ServiceType == typeof(IOrderService) && Equals(e.Key, "primary"));
    }

    [Fact]
    public void Resolve_OpenGenericExposesGenericInterfaceDefinition()
    {
        var exposures = ServiceExposureResolver.Resolve(typeof(Repository<>));

        exposures.Should().Contain(e => e.ServiceType == typeof(IRepository<>) && e.Key == null);
    }
}
