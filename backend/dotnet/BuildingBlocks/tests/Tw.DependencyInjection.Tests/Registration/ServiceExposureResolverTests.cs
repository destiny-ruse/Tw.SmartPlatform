using FluentAssertions;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

public class ServiceExposureResolverTests
{
    private interface IOrderService;
    private interface IRepository<TEntity>;
    private interface IWidgetStore<T>;
    private interface IUnrelatedGeneric<T>;

    private sealed class OrderService : IOrderService, IScopedDependency;

    [ExposeServices(typeof(IOrderService), IncludeSelf = true)]
    private sealed class ExplicitOrderService : IOrderService, IScopedDependency;

    [ExposeServices(typeof(IOrderService))]
    private sealed class ExplicitNoSelfService : IOrderService, IScopedDependency;

    [ExposeKeyedService(typeof(IOrderService), "primary")]
    private sealed class KeyedOrderService : IOrderService, IScopedDependency;

    private sealed class Repository<TEntity> : IRepository<TEntity>, IScopedDependency;

    private sealed class WidgetStore<T> : IWidgetStore<T>, IUnrelatedGeneric<T>, IScopedDependency;

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

    /// <summary>
    /// 显式 ExposeServices 不含 IncludeSelf 时不暴露自身类型
    /// </summary>
    [Fact]
    public void Resolve_ExplicitWithoutIncludeSelf_DoesNotExposeSelf()
    {
        var exposures = ServiceExposureResolver.Resolve(typeof(ExplicitNoSelfService));

        exposures.Should().Contain(e => e.ServiceType == typeof(IOrderService) && e.Key == null);
        exposures.Should().NotContain(e => e.ServiceType == typeof(ExplicitNoSelfService));
    }

    /// <summary>
    /// 显式 ExposeServices 路径不额外追加默认命名匹配接口之外的暴露，结果恰为声明项
    /// </summary>
    [Fact]
    public void Resolve_ExplicitPath_DoesNotAppendDefaultInterfaceExposures()
    {
        var exposures = ServiceExposureResolver.Resolve(typeof(ExplicitOrderService));

        // 显式声明 IOrderService + IncludeSelf=true → 恰好两项，不走默认接口扫描
        exposures.Should().HaveCount(2);
        exposures.Should().Contain(e => e.ServiceType == typeof(IOrderService) && e.Key == null);
        exposures.Should().Contain(e => e.ServiceType == typeof(ExplicitOrderService) && e.Key == null);
    }

    /// <summary>
    /// keyed 暴露与默认路径并存：默认路径暴露自身与命名匹配接口，keyed 独立追加，三者同时存在
    /// </summary>
    /// <remarks>
    /// ABP 风格结尾匹配规则下，KeyedOrderService 命中 IOrderService（"KeyedOrderService".EndsWith("OrderService") 成立），
    /// 因此默认路径暴露自身（Key=null）和非 keyed 的 IOrderService（Key=null）。
    /// ExposeKeyedServiceAttribute 独立追加 keyed IOrderService（Key="primary"）。
    /// </remarks>
    [Fact]
    public void Resolve_KeyedAndDefault_BothPresent()
    {
        var exposures = ServiceExposureResolver.Resolve(typeof(KeyedOrderService));

        // 默认路径：暴露自身
        exposures.Should().Contain(e => e.ServiceType == typeof(KeyedOrderService) && e.Key == null);
        // 默认路径：ABP 结尾匹配，暴露非 keyed 的 IOrderService
        exposures.Should().Contain(e => e.ServiceType == typeof(IOrderService) && e.Key == null);
        // keyed 独立追加
        exposures.Should().Contain(e => e.ServiceType == typeof(IOrderService) && Equals(e.Key, "primary"));
    }

    /// <summary>
    /// 非命名匹配的开放泛型接口不被默认暴露（验证 IsOpenGenericContract 分支已删除）
    /// </summary>
    [Fact]
    public void Resolve_OpenGeneric_DoesNotExposeNonNameMatchedGenericInterface()
    {
        var exposures = ServiceExposureResolver.Resolve(typeof(WidgetStore<>));

        // IWidgetStore<> 命名匹配（IWidgetStore == "I" + "WidgetStore"），应被暴露
        exposures.Should().Contain(e => e.ServiceType == typeof(IWidgetStore<>) && e.Key == null);
        // IUnrelatedGeneric<> 命名不匹配，不应被暴露
        exposures.Should().NotContain(e => e.ServiceType == typeof(IUnrelatedGeneric<>));
    }
}
