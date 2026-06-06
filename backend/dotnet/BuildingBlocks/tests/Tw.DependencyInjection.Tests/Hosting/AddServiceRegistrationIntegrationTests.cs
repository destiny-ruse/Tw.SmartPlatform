using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Diagnostics;
using Tw.DependencyInjection.Discovery;
using Tw.DependencyInjection.Tests.Fixtures;
using Xunit;

namespace Tw.DependencyInjection.Tests.Hosting;

public class AddServiceRegistrationIntegrationTests
{
    private sealed class FakeAssemblySource(params Assembly[] assemblies) : IAssemblySource
    {
        public IReadOnlyList<Assembly> GetCandidateAssemblies() => assemblies;
    }

    private static readonly Assembly FixtureAssembly = typeof(OrderService).Assembly;

    [Fact]
    public void AddServiceRegistration_RegistersDiscoveredServicesAndReport()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddServiceRegistration(configuration, new FakeAssemblySource(FixtureAssembly));
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOrderService>().Should().BeOfType<OrderService>();
        provider.GetRequiredKeyedService<IPaymentProvider>("wechat").Should().BeOfType<WechatPaymentProvider>();
        provider.GetRequiredService<CheckoutService>().Provider.Should().BeOfType<WechatPaymentProvider>();
        provider.GetRequiredService<ServiceRegistrationReport>().Registrations.Should().NotBeEmpty();
    }
}
