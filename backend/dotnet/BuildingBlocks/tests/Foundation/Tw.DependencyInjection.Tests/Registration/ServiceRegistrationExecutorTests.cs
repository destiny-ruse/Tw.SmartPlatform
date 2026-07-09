using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Diagnostics;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

public class ServiceRegistrationExecutorTests
{
    private interface IOrderService;
    private sealed class OrderService : IOrderService;
    private sealed class SomeOtherOrderService : IOrderService;
    private interface IPaymentProvider;
    private sealed class WechatPaymentProvider : IPaymentProvider;
    private sealed class AlipayPaymentProvider : IPaymentProvider;
    private interface IGenericKeyedContract<T>;
    private sealed class GenericKeyedImpl<T> : IGenericKeyedContract<T>;

    [Fact]
    public void Apply_RegistersNonKeyedWinner()
    {
        var services = new ServiceCollection();
        var plan = CreatePlan(new ServiceCandidate(
            typeof(IOrderService),
            typeof(OrderService),
            Key: null,
            DependencyLifetime.Scoped,
            AssemblyName: "Sample",
            TopologyLevel: 0,
            AssemblyPriority: 0,
            TypePriority: 0,
            FinalPriority: 0,
            DiscoveryOrder: 0));

        ServiceRegistrationExecutor.Apply(services, plan);

        services.Should().ContainSingle(d => d.ServiceType == typeof(IOrderService));
    }

    [Fact]
    public void Apply_RegistersKeyedServiceAndEnumerableEntry()
    {
        var services = new ServiceCollection();
        var plan = CreatePlan(new ServiceCandidate(
            typeof(IPaymentProvider),
            typeof(WechatPaymentProvider),
            Key: "wechat",
            DependencyLifetime.Scoped,
            AssemblyName: "Sample",
            TopologyLevel: 0,
            AssemblyPriority: 0,
            TypePriority: 0,
            FinalPriority: 0,
            DiscoveryOrder: 0));

        ServiceRegistrationExecutor.Apply(services, plan);
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredKeyedService<IPaymentProvider>("wechat")
            .Should().BeOfType<WechatPaymentProvider>();
        provider.GetServices<KeyedServiceEntry<IPaymentProvider>>()
            .Should().ContainSingle(e => Equals(e.Key, "wechat") && e.Service is WechatPaymentProvider);
    }

    [Fact]
    public void Apply_ReplacesExistingNonKeyedDescriptor()
    {
        var services = new ServiceCollection();
        // 预置一个既有非 keyed 描述符，模拟容器已注册了旧实现
        services.AddScoped<IOrderService, SomeOtherOrderService>();

        var plan = CreatePlan(new ServiceCandidate(
            typeof(IOrderService),
            typeof(OrderService),
            Key: null,
            DependencyLifetime.Scoped,
            AssemblyName: "Sample",
            TopologyLevel: 0,
            AssemblyPriority: 0,
            TypePriority: 0,
            FinalPriority: 0,
            DiscoveryOrder: 0));

        ServiceRegistrationExecutor.Apply(services, plan);

        // Apply 后旧描述符应被移除，只保留最终选中实现，验证"替换而非追加"语义
        var descriptor = services.Should().ContainSingle(d => d.ServiceType == typeof(IOrderService))
            .Which;
        descriptor.ImplementationType.Should().Be(typeof(OrderService));
    }

    [Fact]
    public void Apply_RegistersMultipleKeyedEntries()
    {
        var services = new ServiceCollection();
        var plan = CreatePlan(
            new ServiceCandidate(
                typeof(IPaymentProvider),
                typeof(WechatPaymentProvider),
                Key: "wechat",
                DependencyLifetime.Scoped,
                AssemblyName: "Sample",
                TopologyLevel: 0,
                AssemblyPriority: 0,
                TypePriority: 0,
                FinalPriority: 0,
                DiscoveryOrder: 0),
            new ServiceCandidate(
                typeof(IPaymentProvider),
                typeof(AlipayPaymentProvider),
                Key: "alipay",
                DependencyLifetime.Scoped,
                AssemblyName: "Sample",
                TopologyLevel: 0,
                AssemblyPriority: 0,
                TypePriority: 0,
                FinalPriority: 0,
                DiscoveryOrder: 1));

        ServiceRegistrationExecutor.Apply(services, plan);
        using var provider = services.BuildServiceProvider();

        // 验证可枚举条目包含两个 key，且各自携带正确的 key 与实现类型
        var entries = provider.GetServices<KeyedServiceEntry<IPaymentProvider>>().ToList();
        entries.Should().HaveCount(2);
        entries.Should().Contain(e => Equals(e.Key, "wechat") && e.Service is WechatPaymentProvider);
        entries.Should().Contain(e => Equals(e.Key, "alipay") && e.Service is AlipayPaymentProvider);

        // 验证两个 keyed 服务各自可正确解析
        provider.GetRequiredKeyedService<IPaymentProvider>("wechat")
            .Should().BeOfType<WechatPaymentProvider>();
        provider.GetRequiredKeyedService<IPaymentProvider>("alipay")
            .Should().BeOfType<AlipayPaymentProvider>();
    }

    [Fact]
    public void Apply_DoesNotThrow_ForKeyedOpenGenericContract()
    {
        // keyed + 开放泛型契约（IGenericKeyedContract<>）不应在 AddKeyedEntry 中因 MakeGenericType 抛 ArgumentException
        var services = new ServiceCollection();
        var plan = CreatePlan(new ServiceCandidate(
            typeof(IGenericKeyedContract<>),
            typeof(GenericKeyedImpl<>),
            Key: "k",
            DependencyLifetime.Scoped,
            AssemblyName: "Sample",
            TopologyLevel: 0,
            AssemblyPriority: 0,
            TypePriority: 0,
            FinalPriority: 0,
            DiscoveryOrder: 0));

        var act = () => ServiceRegistrationExecutor.Apply(services, plan);

        // 不抛异常，且 keyed 描述符已写入（开放泛型 keyed 服务本身可正常注册）
        act.Should().NotThrow();
        services.Should().Contain(d =>
            d.ServiceType == typeof(IGenericKeyedContract<>) &&
            Equals(d.ServiceKey, "k"));
    }

    [Fact]
    public void Apply_Throws_WhenServicesNull()
    {
        var plan = CreatePlan();

        var act = () => ServiceRegistrationExecutor.Apply(null!, plan);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void Apply_Throws_WhenPlanNull()
    {
        var services = new ServiceCollection();

        var act = () => ServiceRegistrationExecutor.Apply(services, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("plan");
    }

    private static ServiceRegistrationPlan CreatePlan(params ServiceCandidate[] registrations)
    {
        return new ServiceRegistrationPlan(
            registrations,
            new ServiceRegistrationReport([], [], []));
    }
}
