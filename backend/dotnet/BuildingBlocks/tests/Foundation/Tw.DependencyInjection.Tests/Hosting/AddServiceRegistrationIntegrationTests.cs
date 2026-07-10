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

/// <summary>
/// 覆盖添加服务RegistrationIntegration的核心行为和边界条件
/// </summary>
public class AddServiceRegistrationIntegrationTests
{
    /// <summary>
    /// 覆盖FakeAssemblySource的核心行为和边界条件
    /// </summary>
    private sealed class FakeAssemblySource(params Assembly[] assemblies) : IAssemblySource
    {
        /// <summary>
        /// 说明读取CandidateAssemblies在当前类型中的职责
        /// </summary>
        /// <returns>匹配当前查询条件的结果集合</returns>
        public IReadOnlyList<Assembly> GetCandidateAssemblies() => assemblies;
    }

    /// <summary>
    /// 保存当前类型处理流程依赖的FixtureAssembly
    /// </summary>
    private static readonly Assembly FixtureAssembly = typeof(OrderService).Assembly;

    /// <summary>
    /// 验证添加服务Registration注册DiscoveredServices和报告
    /// </summary>
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

    /// <summary>
    /// 验证添加服务Registration注册OpenGenericContract
    /// </summary>
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

    /// <summary>
    /// 验证添加服务RegistrationBinds选项和注册选项报告
    /// </summary>
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

    /// <summary>
    /// 定义缺少提供器的能力边界
    /// </summary>
    private interface IMissingProvider;

    /// <summary>
    /// 覆盖缺少键Consumer的核心行为和边界条件
    /// </summary>
    private sealed class MissingKeyConsumer : IScopedDependency
    {
        /// <summary>
        /// 初始化 MissingKeyConsumer 实例
        /// </summary>
        /// <param name="provider">用于提供provider</param>
        public MissingKeyConsumer([FromKeyedServices("missing")] IMissingProvider provider)
        {
            Provider = provider;
        }

        /// <summary>
        /// 提供器在当前对象中的业务含义
        /// </summary>
        public IMissingProvider Provider { get; }
    }

    /// <summary>
    /// 验证构造函数Keyed服务Validator抛出异常当FromKeyedServicesReferences缺少键
    /// </summary>
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

    /// <summary>
    /// 说明ConfigurationForFixtures在当前类型中的职责
    /// </summary>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static IConfiguration ConfigurationForFixtures() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IntegrationCache:Endpoint"] = "localhost",
                ["Tw:Redis:Endpoint"] = "redis",
            })
            .Build();
}
