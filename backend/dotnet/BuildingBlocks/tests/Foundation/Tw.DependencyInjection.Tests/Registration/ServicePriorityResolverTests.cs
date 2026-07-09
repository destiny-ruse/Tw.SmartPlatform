using System.Reflection;
using AwesomeAssertions;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

/// <summary>验证 ServicePriorityResolverTests 相关行为</summary>
public class ServicePriorityResolverTests
{
    /// <summary>验证 TypePriorityService 相关行为</summary>
    [ServicePriority(20)]
    [ServiceRegistration(Priority = 20)]
    private sealed class TypePriorityService;

    /// <summary>验证 ConflictingTypePriorityService 相关行为</summary>
    [ServicePriority(20)]
    [ServiceRegistration(Priority = 10)]
    private sealed class ConflictingTypePriorityService;

    /// <summary>验证 RegistrationPriorityOnlyService 相关行为</summary>
    [ServiceRegistration(Priority = 15)]
    private sealed class RegistrationPriorityOnlyService;

    /// <summary>验证 ServicePriorityOnlyService 相关行为</summary>
    [ServicePriority(7)]
    private sealed class ServicePriorityOnlyService;

    /// <summary>验证 OutOfRangeTypePriorityService 相关行为</summary>
    [ServicePriority(200_000)]
    private sealed class OutOfRangeTypePriorityService;

    /// <summary>验证 LifetimeWithSeparatePriorityService 相关行为</summary>
    [ServiceRegistration(DependencyLifetime.Singleton)]
    [ServicePriority(5)]
    private sealed class LifetimeWithSeparatePriorityService;

    /// <summary>验证 ResolveTypePriority_UsesExplicitPriority 场景</summary>
    [Fact]
    public void ResolveTypePriority_UsesExplicitPriority()
    {
        ServicePriorityResolver.ResolveTypePriority(typeof(TypePriorityService)).Should().Be(20);
    }

    /// <summary>验证 ResolveTypePriority_FailsWhenTwoAttributesDisagree 场景</summary>
    [Fact]
    public void ResolveTypePriority_FailsWhenTwoAttributesDisagree()
    {
        var act = () => ServicePriorityResolver.ResolveTypePriority(typeof(ConflictingTypePriorityService));

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*类型优先级声明不一致*");
    }

    /// <summary>验证 ResolveAssemblyPriority_ConfigOverridesAttribute 场景</summary>
    [Fact]
    public void ResolveAssemblyPriority_ConfigOverridesAttribute()
    {
        var options = new ServiceRegistrationOptions();
        options.AssemblyPriorities.Add(typeof(TypePriorityService).Assembly.GetName().Name!, 50);

        ServicePriorityResolver.ResolveAssemblyPriority(typeof(TypePriorityService).Assembly, options)
            .Should().Be(50);
    }

    /// <summary>验证 CalculateFinalPriority_UsesTopologyBaseAssemblyAndTypePriority 场景</summary>
    [Fact]
    public void CalculateFinalPriority_UsesTopologyBaseAssemblyAndTypePriority()
    {
        ServicePriorityResolver.CalculateFinalPriority(topologyLevel: 2, assemblyPriority: 30, typePriority: 40)
            .Should().Be(2_000_070);
    }

    // ──────────────────────────────────────────────────────────
    // 仅 ServiceRegistrationAttribute.Priority
    // ──────────────────────────────────────────────────────────

    /// <summary>验证 ResolveTypePriority_UsesRegistrationPriority_WhenOnlyServiceRegistrationAttribute 场景</summary>
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

    /// <summary>验证 ResolveTypePriority_UsesServicePriority_WhenOnlyServicePriorityAttribute 场景</summary>
    [Fact]
    public void ResolveTypePriority_UsesServicePriority_WhenOnlyServicePriorityAttribute()
    {
        // 只有 [ServicePriority(7)]，没有 [ServiceRegistration]
        ServicePriorityResolver.ResolveTypePriority(typeof(ServicePriorityOnlyService))
            .Should().Be(7);
    }

    // ──────────────────────────────────────────────────────────
    // 越界类型优先级 → 抛异常
    // ──────────────────────────────────────────────────────────

    /// <summary>验证 ResolveTypePriority_Throws_WhenPriorityOutOfRange 场景</summary>
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

    /// <summary>验证 CalculateFinalPriority_Throws_WhenExplicitPriorityOutOfRange 场景</summary>
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

    /// <summary>验证 ResolveTypePriority_AllowsServiceRegistrationLifetimeWithSeparateServicePriority 场景</summary>
    [Fact]
    public void ResolveTypePriority_AllowsServiceRegistrationLifetimeWithSeparateServicePriority()
    {
        // [ServiceRegistration(DependencyLifetime.Singleton)] 用于声明生命周期（Priority 默认 0，视为中性/未设置）
        // [ServicePriority(5)] 用于独立声明类型优先级，是文档推荐的合法组合，不应误报"类型优先级声明不一致"
        ServicePriorityResolver.ResolveTypePriority(typeof(LifetimeWithSeparatePriorityService))
            .Should().Be(5);
    }

}
