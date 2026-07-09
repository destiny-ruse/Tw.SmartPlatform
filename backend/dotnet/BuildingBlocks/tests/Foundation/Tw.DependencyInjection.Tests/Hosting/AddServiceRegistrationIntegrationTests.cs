using System.Reflection;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Diagnostics;
using Tw.DependencyInjection.Discovery;
using Tw.DependencyInjection.Registration;
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
        var configuration = ConfigurationForFixtures();

        services.AddServiceRegistration(configuration, new FakeAssemblySource(FixtureAssembly));
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOrderService>().Should().BeOfType<OrderService>();
        provider.GetRequiredKeyedService<IPaymentProvider>("wechat").Should().BeOfType<WechatPaymentProvider>();
        provider.GetRequiredService<CheckoutService>().Provider.Should().BeOfType<WechatPaymentProvider>();
        provider.GetRequiredService<ServiceRegistrationReport>().Registrations.Should().NotBeEmpty();
    }

    [Fact]
    public void AddServiceRegistration_RegistersOpenGenericContract()
    {
        var services = new ServiceCollection();
        var configuration = ConfigurationForFixtures();

        services.AddServiceRegistration(configuration, new FakeAssemblySource(FixtureAssembly));
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IRepository<OrderEntity>>()
            .Should().BeOfType<Repository<OrderEntity>>();
    }

    [Fact]
    public void AddServiceRegistration_BindsOptionsAndRegistersOptionsReport()
    {
        var services = new ServiceCollection();
        var configuration = ConfigurationForFixtures();

        services.AddServiceRegistration(configuration, new FakeAssemblySource(FixtureAssembly));
        using var provider = services.BuildServiceProvider(validateScopes: true);

        var cache = provider.GetRequiredService<IOptions<IntegrationCacheOptions>>().Value;
        cache.Endpoint.Should().Be("localhost");
        cache.EffectiveEndpoint.Should().Be("localhost");
        provider.GetRequiredService<IOptionsMonitor<NamedRedisOptions>>()
            .Get("primary")
            .Endpoint
            .Should()
            .Be("redis");
        provider.GetRequiredService<OptionsBindingReport>().Items.Should()
            .Contain(item => item.SectionPath == "IntegrationCache");
    }

    private interface IMissingProvider;

    private sealed class MissingKeyConsumer : IScopedDependency
    {
        public MissingKeyConsumer([FromKeyedServices("missing")] IMissingProvider provider)
        {
            Provider = provider;
        }

        public IMissingProvider Provider { get; }
    }

    [Fact]
    public void ConstructorKeyedServiceValidator_ThrowsWhenFromKeyedServicesReferencesMissingKey()
    {
        var plan = ServiceRegistrationPlanner.Plan(
            assemblies: [typeof(MissingKeyConsumer).Assembly],
            typesByAssemblyName: new Dictionary<string, IReadOnlyList<Type>>(StringComparer.Ordinal)
            {
                [typeof(MissingKeyConsumer).Assembly.GetName().Name!] = [typeof(MissingKeyConsumer)],
            },
            topologyLevelsByAssemblyName: new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [typeof(MissingKeyConsumer).Assembly.GetName().Name!] = 0,
            },
            reachabilityGraph: new AssemblyReachabilityGraph(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [typeof(MissingKeyConsumer).Assembly.GetName().Name!] = [],
            }),
            options: new ServiceRegistrationOptions());

        var act = () => ConstructorKeyedServiceValidator.Validate(plan.Registrations);

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*未注册 keyed service*");
    }

    private static IConfiguration ConfigurationForFixtures() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IntegrationCache:Endpoint"] = "localhost",
                ["Tw:Redis:Endpoint"] = "redis",
            })
            .Build();
}
