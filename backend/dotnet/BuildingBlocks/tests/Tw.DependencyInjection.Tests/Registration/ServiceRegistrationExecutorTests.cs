using FluentAssertions;
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
    private interface IPaymentProvider;
    private sealed class WechatPaymentProvider : IPaymentProvider;

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

    private static ServiceRegistrationPlan CreatePlan(params ServiceCandidate[] registrations)
    {
        return new ServiceRegistrationPlan(
            registrations,
            new ServiceRegistrationReport([], [], []));
    }
}
