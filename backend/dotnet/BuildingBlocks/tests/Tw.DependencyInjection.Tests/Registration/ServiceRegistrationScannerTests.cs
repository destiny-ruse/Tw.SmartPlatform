using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Core.Exceptions;
using Tw.DependencyInjection.Registration;
using Tw.DependencyInjection.Tests.Registration.Fixtures;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

/// <summary>
/// 验证 ServiceRegistrationScanner 的元数据扫描与暴露规则。
/// </summary>
public sealed class ServiceRegistrationScannerTests
{
    // ---- helpers ----

    private static IReadOnlyList<ServiceScanDescriptor> Scan(Type implementationType)
        => ServiceRegistrationScanner.ScanType(implementationType);

    // ============================================================
    // Step 1: 生命周期检测与默认暴露规则
    // ============================================================

    [Fact]
    public void Scanner_Exposes_Business_Interfaces_For_Lifecycle_Implementations()
    {
        var descriptors = Scan(typeof(OrderService));

        descriptors.Should().ContainSingle()
            .Which.ServiceTypes.Should().ContainSingle(type => type == typeof(IOrderService));
    }

    [Fact]
    public void Scanner_Exposes_Implementation_Type_When_No_Business_Interface_Exists()
    {
        var descriptors = Scan(typeof(ConcreteWorker));

        descriptors.Should().ContainSingle()
            .Which.ServiceTypes.Should().ContainSingle(type => type == typeof(ConcreteWorker));
    }

    [Fact]
    public void Scanner_Detects_Scoped_Lifetime()
    {
        var descriptors = Scan(typeof(OrderService));

        descriptors.Should().ContainSingle()
            .Which.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void Scanner_Detects_Transient_Lifetime()
    {
        var descriptors = Scan(typeof(FooBar));

        descriptors.Should().ContainSingle()
            .Which.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void Scanner_Detects_Singleton_Lifetime()
    {
        var descriptors = Scan(typeof(SingletonPayment));

        descriptors.Should().ContainSingle()
            .Which.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void Scanner_Returns_Empty_For_Type_Without_Lifecycle_Marker()
    {
        // A plain class with no lifecycle marker interface should produce no descriptor.
        var descriptors = Scan(typeof(object));

        descriptors.Should().BeEmpty();
    }

    [Fact]
    public void Scanner_Returns_Empty_For_Interface_Type()
    {
        var descriptors = Scan(typeof(IOrderService));

        descriptors.Should().BeEmpty();
    }

    [Fact]
    public void Scanner_Returns_Empty_For_Abstract_Type()
    {
        var descriptors = Scan(typeof(AbstractService));

        descriptors.Should().BeEmpty();
    }

    // ============================================================
    // Step 2: 显式元数据属性
    // ============================================================

    [Fact]
    public void Scanner_Returns_Empty_For_Disabled_Type()
    {
        var descriptors = Scan(typeof(DisabledService));

        descriptors.Should().BeEmpty();
    }

    [Fact]
    public void Scanner_Uses_ExposeServices_Attribute_When_Present()
    {
        var descriptors = Scan(typeof(FooBar));

        descriptors.Should().ContainSingle()
            .Which.ServiceTypes.Should().ContainSingle(t => t == typeof(IFoo));
    }

    [Fact]
    public void Scanner_ExposeServices_Excludes_Unlisted_Interface()
    {
        var descriptors = Scan(typeof(FooBar));

        descriptors.Should().ContainSingle()
            .Which.ServiceTypes.Should().NotContain(typeof(IBar));
    }

    [Fact]
    public void Scanner_IncludeSelf_Appends_Implementation_Type()
    {
        var descriptors = Scan(typeof(FooBarSelf));

        var serviceTypes = descriptors.Should().ContainSingle().Which.ServiceTypes;
        serviceTypes.Should().Contain(typeof(IFoo));
        serviceTypes.Should().Contain(typeof(FooBarSelf));
    }

    [Fact]
    public void Scanner_Detects_KeyedService_Attribute()
    {
        var descriptors = Scan(typeof(PremiumPayment));

        descriptors.Should().ContainSingle()
            .Which.Key.Should().Be("premium");
    }

    [Fact]
    public void Scanner_Keyed_Descriptor_Exposes_Business_Interface()
    {
        var descriptors = Scan(typeof(PremiumPayment));

        descriptors.Should().ContainSingle()
            .Which.ServiceTypes.Should().Contain(typeof(IPaymentService));
    }

    [Fact]
    public void Scanner_Detects_CollectionService_Attribute()
    {
        var descriptors = Scan(typeof(FirstHandler));

        var descriptor = descriptors.Should().ContainSingle().Which;
        descriptor.IsCollection.Should().BeTrue();
        descriptor.Order.Should().Be(10);
    }

    [Fact]
    public void Scanner_Detects_ReplaceService_Attribute()
    {
        var descriptors = Scan(typeof(ReplacementOrderService));

        var descriptor = descriptors.Should().ContainSingle().Which;
        descriptor.IsReplacement.Should().BeTrue();
        descriptor.Order.Should().Be(5);
    }

    [Fact]
    public void Scanner_Supports_Open_Generic_Type_Definition()
    {
        // Repository<T> is a valid open generic + ExposeServices(typeof(IRepository<>))
        var descriptors = Scan(typeof(Repository<>));

        var descriptor = descriptors.Should().ContainSingle().Which;
        descriptor.IsOpenGenericDefinition.Should().BeTrue();
        descriptor.ServiceTypes.Should().Contain(typeof(IRepository<>));
    }

    [Fact]
    public void Scanner_Rejects_Open_Generic_Keyed_Service()
    {
        var act = () => Scan(typeof(KeyedRepository<>));

        act.Should().Throw<TwConfigurationException>()
            .WithMessage("*开放泛型*KeyedService*不支持*");
    }

    [Fact]
    public void Scanner_Rejects_Multiple_Lifecycle_Markers()
    {
        var act = () => Scan(typeof(MultiLifecycleService));

        act.Should().Throw<TwConfigurationException>()
            .WithMessage("*MultiLifecycleService*");
    }

    [Fact]
    public void Scanner_AssemblyName_Is_Populated()
    {
        var descriptors = Scan(typeof(OrderService));

        descriptors.Should().ContainSingle()
            .Which.AssemblyName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Scanner_TransientWorker_Exposes_Itself_When_No_Business_Interface()
    {
        var descriptors = Scan(typeof(TransientWorker));

        descriptors.Should().ContainSingle()
            .Which.ServiceTypes.Should().ContainSingle(t => t == typeof(TransientWorker));
    }

    // ============================================================
    // Helpers — abstract base type fixture (defined inline)
    // ============================================================

    public abstract class AbstractService : IScopedDependency { }
}
