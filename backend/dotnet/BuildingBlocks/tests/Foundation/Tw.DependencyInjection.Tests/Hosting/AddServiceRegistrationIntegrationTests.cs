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

/// <summary>验证 AddServiceRegistrationIntegrationTests 相关行为</summary>
public class AddServiceRegistrationIntegrationTests
{
    /// <summary>验证 FakeAssemblySource 相关行为</summary>
    private sealed class FakeAssemblySource(params Assembly[] assemblies) : IAssemblySource
    {
        /// <summary>验证 GetCandidateAssemblies 场景</summary>
        /// <returns>GetCandidateAssemblies 的执行结果</returns>
        public IReadOnlyList<Assembly> GetCandidateAssemblies() => assemblies;
    }

    /// <summary>表示 FixtureAssembly 字段</summary>
    private static readonly Assembly FixtureAssembly = typeof(OrderService).Assembly;

    /// <summary>验证 AddServiceRegistration_RegistersDiscoveredServicesAndReport 场景</summary>
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

    /// <summary>验证 AddServiceRegistration_RegistersOpenGenericContract 场景</summary>
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

    /// <summary>验证 AddServiceRegistration_BindsOptionsAndRegistersOptionsReport 场景</summary>
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

    /// <summary>定义 IMissingProvider 契约</summary>
    private interface IMissingProvider;

    /// <summary>验证 MissingKeyConsumer 相关行为</summary>
    private sealed class MissingKeyConsumer : IScopedDependency
    {
        /// <summary>初始化 MissingKeyConsumer 实例</summary>
        /// <param name="provider">provider 参数</param>
        public MissingKeyConsumer([FromKeyedServices("missing")] IMissingProvider provider)
        {
            Provider = provider;
        }

        /// <summary>表示 Provider 属性</summary>
        public IMissingProvider Provider { get; }
    }

    /// <summary>验证 ConstructorKeyedServiceValidator_ThrowsWhenFromKeyedServicesReferencesMissingKey 场景</summary>
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

    /// <summary>验证 ConfigurationForFixtures 场景</summary>
    /// <returns>ConfigurationForFixtures 的执行结果</returns>
    private static IConfiguration ConfigurationForFixtures() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IntegrationCache:Endpoint"] = "localhost",
                ["Tw:Redis:Endpoint"] = "redis",
            })
            .Build();
}
