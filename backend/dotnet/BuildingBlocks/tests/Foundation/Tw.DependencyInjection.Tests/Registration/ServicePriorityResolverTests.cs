using System.Reflection;
using System.Reflection.Emit;
using AwesomeAssertions;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

/// <summary>
/// 覆盖服务PriorityResolver的核心行为和边界条件
/// </summary>
public class ServicePriorityResolverTests
{
    /// <summary>
    /// 覆盖类型Priority服务的核心行为和边界条件
    /// </summary>
    [ServicePriority(20)]
    [ServiceRegistration(Priority = 20)]
    private sealed class TypePriorityService;

    /// <summary>
    /// 覆盖Conflicting类型Priority服务的核心行为和边界条件
    /// </summary>
    [ServicePriority(20)]
    [ServiceRegistration(Priority = 10)]
    private sealed class ConflictingTypePriorityService;

    /// <summary>
    /// 覆盖RegistrationPriorityOnly服务的核心行为和边界条件
    /// </summary>
    [ServiceRegistration(Priority = 15)]
    private sealed class RegistrationPriorityOnlyService;

    /// <summary>
    /// 覆盖服务PriorityOnly服务的核心行为和边界条件
    /// </summary>
    [ServicePriority(7)]
    private sealed class ServicePriorityOnlyService;

    /// <summary>
    /// 覆盖OutOfRange类型Priority服务的核心行为和边界条件
    /// </summary>
    [ServicePriority(200_000)]
    private sealed class OutOfRangeTypePriorityService;

    /// <summary>
    /// 覆盖Lifetime使用SeparatePriority服务的核心行为和边界条件
    /// </summary>
    [ServiceRegistration(DependencyLifetime.Singleton)]
    [ServicePriority(5)]
    private sealed class LifetimeWithSeparatePriorityService;

    /// <summary>
    /// 验证Resolve类型PriorityUsesExplicitPriority
    /// </summary>
    [Fact]
    public void ResolveTypePriority_UsesExplicitPriority()
    {
        ServicePriorityResolver.ResolveTypePriority(typeof(TypePriorityService)).Should().Be(20);
    }

    /// <summary>
    /// 验证Resolve类型PriorityFails当TwoAttributesDisagree
    /// </summary>
    [Fact]
    public void ResolveTypePriority_FailsWhenTwoAttributesDisagree()
    {
        var act = () => ServicePriorityResolver.ResolveTypePriority(typeof(ConflictingTypePriorityService));

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*类型优先级声明不一致*");
    }

    /// <summary>
    /// 验证配置优先级覆盖程序集特性声明
    /// </summary>
    [Fact]
    public void ResolveAssemblyPriority_ConfigWinsOverAssemblyRegistrationPriorityAttribute()
    {
        var assembly = CreateAssemblyWithRegistrationPriority(priority: 25);
        var options = new ServiceRegistrationOptions();
        options.AssemblyPriorities.Add(assembly.GetName().Name!, 50);

        ServicePriorityResolver.ResolveAssemblyPriority(assembly, options)
            .Should().Be(50);
    }

    /// <summary>
    /// 验证未配置优先级时使用程序集级 AssemblyRegistrationPriorityAttribute
    /// </summary>
    [Fact]
    public void ResolveAssemblyPriority_UsesAssemblyRegistrationPriorityAttribute_WhenNotConfigured()
    {
        var assembly = CreateAssemblyWithRegistrationPriority(priority: 25);

        ServicePriorityResolver.ResolveAssemblyPriority(assembly, new ServiceRegistrationOptions())
            .Should().Be(25);
    }

    /// <summary>
    /// 验证CalculateFinalPriorityUsesTopology基类Assembly和类型Priority
    /// </summary>
    [Fact]
    public void CalculateFinalPriority_UsesTopologyBaseAssemblyAndTypePriority()
    {
        ServicePriorityResolver.CalculateFinalPriority(topologyLevel: 2, assemblyPriority: 30, typePriority: 40)
            .Should().Be(2_000_070);
    }

    // ──────────────────────────────────────────────────────────
    // 仅 ServiceRegistrationAttribute.Priority
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// 验证Resolve类型PriorityUsesRegistrationPriority当Only服务Registration特性
    /// </summary>
    [Fact]
    public void ResolveTypePriority_UsesRegistrationPriority_WhenOnlyServiceRegistrationAttribute()
    {
        // 只有 [ServiceRegistration(Priority = 15)]，没有 [ServicePriority]
        ServicePriorityResolver.ResolveTypePriority(typeof(RegistrationPriorityOnlyService))
            .Should().Be(15);
    }

    // ──────────────────────────────────────────────────────────
    // 仅 ServicePriorityAttribute
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// 验证类型级 ServicePriorityAttribute 的优先级行为保持不变
    /// </summary>
    [Fact]
    public void ResolveTypePriority_UsesServicePriorityAttribute_WhenOnlyServicePriorityAttribute()
    {
        // 只有 [ServicePriority(7)]，没有 [ServiceRegistration]
        ServicePriorityResolver.ResolveTypePriority(typeof(ServicePriorityOnlyService))
            .Should().Be(7);
    }

    // ──────────────────────────────────────────────────────────
    // 越界类型优先级 → 抛异常
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// 验证Resolve类型Priority抛出异常当PriorityOutOfRange
    /// </summary>
    [Fact]
    public void ResolveTypePriority_Throws_WhenPriorityOutOfRange()
    {
        // [ServicePriority(200_000)] 超出允许范围 ±100_000
        var act = () => ServicePriorityResolver.ResolveTypePriority(typeof(OutOfRangeTypePriorityService));

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*超出允许范围*");
    }

    // ──────────────────────────────────────────────────────────
    // CalculateFinalPriority 直接传入越界值 → 抛异常
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// 验证CalculateFinalPriority抛出异常当ExplicitPriorityOutOfRange
    /// </summary>
    [Fact]
    public void CalculateFinalPriority_Throws_WhenExplicitPriorityOutOfRange()
    {
        // assemblyPriority = 200_000 超出允许范围 ±100_000
        var act = () => ServicePriorityResolver.CalculateFinalPriority(0, 200_000, 0);

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*超出允许范围*");
    }

    // ──────────────────────────────────────────────────────────
    // [ServiceRegistration(生命周期)] + [ServicePriority(N)] 合法组合
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// 验证Resolve类型PriorityAllows服务RegistrationLifetime带有Separate服务Priority
    /// </summary>
    [Fact]
    public void ResolveTypePriority_AllowsServiceRegistrationLifetimeWithSeparateServicePriority()
    {
        // [ServiceRegistration(DependencyLifetime.Singleton)] 用于声明生命周期（Priority 默认 0，视为中性/未设置）
        // [ServicePriority(5)] 用于独立声明类型优先级，是文档推荐的合法组合，不应误报"类型优先级声明不一致"
        ServicePriorityResolver.ResolveTypePriority(typeof(LifetimeWithSeparatePriorityService))
            .Should().Be(5);
    }

    /// <summary>
    /// 创建带有程序集注册优先级特性的动态程序集
    /// </summary>
    /// <param name="priority">程序集注册优先级</param>
    /// <returns>带有指定程序集特性的动态程序集</returns>
    private static Assembly CreateAssemblyWithRegistrationPriority(int priority)
    {
        var assemblyName = new AssemblyName($"Tw.DependencyInjection.Tests.Priority.{Guid.NewGuid():N}");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var attributeConstructor = typeof(AssemblyRegistrationPriorityAttribute)
            .GetConstructor([typeof(int)])!;
        assembly.SetCustomAttribute(new CustomAttributeBuilder(attributeConstructor, [priority]));
        return assembly;
    }

}
