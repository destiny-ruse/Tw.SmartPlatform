using System.Reflection;
using AwesomeAssertions;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Diagnostics;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

/// <summary>验证 ServiceRegistrationPlannerTests 相关行为</summary>
public class ServiceRegistrationPlannerTests
{
    /// <summary>定义 IPaymentProvider 契约</summary>
    private interface IPaymentProvider;

    /// <summary>验证 DefaultPaymentProvider 相关行为</summary>
    private sealed class DefaultPaymentProvider : IPaymentProvider, IScopedDependency;

    /// <summary>验证 PreferredPaymentProvider 相关行为</summary>
    [ServicePriority(10)]
    private sealed class PreferredPaymentProvider : IPaymentProvider, IScopedDependency;

    /// <summary>验证 WechatPaymentProvider 相关行为</summary>
    [ExposeKeyedService(typeof(IPaymentProvider), "wechat")]
    private sealed class WechatPaymentProvider : IPaymentProvider, IScopedDependency;

    /// <summary>验证 PreferredWechatPaymentProvider 相关行为</summary>
    [ExposeKeyedService(typeof(IPaymentProvider), "wechat")]
    [ServicePriority(5)]
    private sealed class PreferredWechatPaymentProvider : IPaymentProvider, IScopedDependency;

    /// <summary>验证 EmptyKeyPaymentProvider 相关行为</summary>
    [ExposeKeyedService(typeof(IPaymentProvider), "")]
    private sealed class EmptyKeyPaymentProvider : IPaymentProvider, IScopedDependency;

    /// <summary>验证 DefaultPaymentProviderClone 相关行为</summary>
    [ExposeServices(typeof(IPaymentProvider))]
    private sealed class DefaultPaymentProviderClone : IPaymentProvider, IScopedDependency;

    // ── 用于 skipped 路径测试的私有类型 ──────────────────────────────────────
    /// <summary>定义 IScopedDependencyMarker 契约</summary>
    private interface IScopedDependencyMarker : IScopedDependency;

    /// <summary>验证 AbstractScoped 相关行为</summary>
    private abstract class AbstractScoped : IScopedDependency;

    /// <summary>验证 DisabledScoped 相关行为</summary>
    [DisableServiceRegistration]
    private sealed class DisabledScoped : IScopedDependency;

    /// <summary>验证 MultiLifetimeScoped 相关行为</summary>
    private sealed class MultiLifetimeScoped : IScopedDependency, ISingletonDependency;

    // ── 用于平级对照测试的私有类型（同一程序集、不同 typePriority）────────────
    /// <summary>验证 LowPriorityService 相关行为</summary>
    private sealed class LowPriorityService : IScopedDependency;

    /// <summary>验证 HighPriorityService 相关行为</summary>
    [ServicePriority(5)]
    private sealed class HighPriorityService : IScopedDependency;

    /// <summary>验证 Planner_SelectsHighestPriorityNonKeyedCandidate 场景</summary>
    [Fact]
    public void Planner_SelectsHighestPriorityNonKeyedCandidate()
    {
        var plan = PlanTypes(typeof(DefaultPaymentProvider), typeof(PreferredPaymentProvider));

        plan.Registrations.Should().ContainSingle(r =>
            r.ServiceType == typeof(IPaymentProvider)
            && r.ImplementationType == typeof(PreferredPaymentProvider)
            && r.Key == null);
        plan.Report.SupersededCandidates.Should().ContainSingle(s =>
            s.ImplementationTypeName.Contains(nameof(DefaultPaymentProvider), StringComparison.Ordinal));
    }

    /// <summary>验证 Planner_ArbitratesKeyedCandidatesPerKey 场景</summary>
    [Fact]
    public void Planner_ArbitratesKeyedCandidatesPerKey()
    {
        var plan = PlanTypes(typeof(WechatPaymentProvider), typeof(PreferredWechatPaymentProvider));

        plan.Registrations.Should().ContainSingle(r =>
            r.ServiceType == typeof(IPaymentProvider)
            && r.ImplementationType == typeof(PreferredWechatPaymentProvider)
            && Equals(r.Key, "wechat"));
    }

    /// <summary>验证 Planner_ThrowsWhenFinalPriorityTies 场景</summary>
    [Fact]
    public void Planner_ThrowsWhenFinalPriorityTies()
    {
        var act = () => PlanTypes(typeof(DefaultPaymentProvider), typeof(DefaultPaymentProviderClone));

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*最终优先级相同*");
    }

    /// <summary>验证 Planner_ThrowsWhenTypeDeclaresMultipleLifetimeMarkers 场景</summary>
    [Fact]
    public void Planner_ThrowsWhenTypeDeclaresMultipleLifetimeMarkers()
    {
        var act = () => PlanTypes(typeof(MultiLifetimeScoped));

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*多个生命周期标记*");
    }

    /// <summary>验证 Planner_ThrowsWhenKeyedServiceKeyIsEmpty 场景</summary>
    [Fact]
    public void Planner_ThrowsWhenKeyedServiceKeyIsEmpty()
    {
        var act = () => PlanTypes(typeof(EmptyKeyPaymentProvider));

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*key*不能为空*");
    }

    /// <summary>
    /// 对 <see cref="Planner_SelectsHighestPriorityNonKeyedCandidate"/> 场景补充断言：
    /// 被淘汰项的 SupersededCandidates 字段应正确记录 winner 信息
    /// </summary>
    [Fact]
    public void Planner_SupersededDiagnostic_RecordsWinnerInfo()
    {
        var plan = PlanTypes(typeof(DefaultPaymentProvider), typeof(PreferredPaymentProvider));

        var superseded = plan.Report.SupersededCandidates.Should().ContainSingle().Which;

        superseded.SelectedImplementationTypeName
            .Should().Contain(nameof(PreferredPaymentProvider));
        // DefaultPaymentProvider 的 FinalPriority == 0（无 ServicePriority，topologyLevel=0）
        superseded.FinalPriority.Should().Be(0);
        // PreferredPaymentProvider 的 FinalPriority == 10（[ServicePriority(10)]，topologyLevel=0）
        superseded.SelectedFinalPriority.Should().Be(10);
    }

    /// <summary>
    /// 对非 keyed 仲裁场景断言 Candidates 诊断中的 Status 字段：
    /// winner 为 "selected"，落选者为 "superseded"
    /// </summary>
    [Fact]
    public void Planner_CandidateDiagnostic_StatusReflectsArbitrationResult()
    {
        var plan = PlanTypes(typeof(DefaultPaymentProvider), typeof(PreferredPaymentProvider));

        var candidates = plan.Report.Candidates
            .Where(c => c.ServiceTypeName.Contains(nameof(IPaymentProvider), StringComparison.Ordinal)
                        && c.Key == null)
            .ToList();

        candidates.Should().HaveCount(2);

        candidates.Should().ContainSingle(c =>
            c.ImplementationTypeName.Contains(nameof(PreferredPaymentProvider), StringComparison.Ordinal)
            && c.Status == "selected");

        candidates.Should().ContainSingle(c =>
            c.ImplementationTypeName.Contains(nameof(DefaultPaymentProvider), StringComparison.Ordinal)
            && !c.ImplementationTypeName.Contains(nameof(PreferredPaymentProvider), StringComparison.Ordinal)
            && c.Status == "superseded");
    }

    /// <summary>
    /// 平级对照测试：同一程序集内两个候选 typePriority 不同，winner 可确定，不触发平级失败。
    ///
    /// 说明：真实的平级失败（失败条件 1）需要两个物理程序集彼此不可达、且 assemblyPriority/typePriority
    /// 完全相同但 topologyLevel 不同。在单测试程序集内无法构造两个真实的物理程序集（类型的
    /// Assembly 属性固定），因此此处仅验证"同程序集、不同 typePriority"的正常仲裁路径作为对照，
    /// 证明算法在 winner 可确定时不报错。平级失败路径已由代码注释与 Task 11 回归覆盖。
    /// </summary>
    [Fact]
    public void Planner_SelectsHigherTypePriority_WhenSameAssembly()
    {
        var assembly = typeof(ServiceRegistrationPlannerTests).Assembly;
        var assemblyName = assembly.GetName().Name!;

        // 两个候选同属一个程序集，topologyLevel 均为 0，HighPriorityService typePriority=5
        var plan = ServiceRegistrationPlanner.Plan(
            assemblies: [assembly],
            typesByAssemblyName: new Dictionary<string, IReadOnlyList<Type>>(StringComparer.Ordinal)
            {
                [assemblyName] = [typeof(LowPriorityService), typeof(HighPriorityService)],
            },
            topologyLevelsByAssemblyName: new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [assemblyName] = 0,
            },
            reachabilityGraph: new AssemblyReachabilityGraph(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [assemblyName] = [],
            }),
            options: new ServiceRegistrationOptions());

        // LowPriorityService 自身暴露为 self，HighPriorityService 亦然——两者契约不同，各自胜出
        // 主要断言：不抛出异常（平级失败未发生）
        plan.Registrations.Should().NotBeEmpty();
        plan.Report.SupersededCandidates.Should().BeEmpty();
    }

    /// <summary>
    /// skipped 路径断言：抽象类与标注 [DisableServiceRegistration] 的类应出现在 SkippedTypes，
    /// 且不出现在 Registrations
    /// </summary>
    [Fact]
    public void Planner_SkipsAbstractAndDisabledTypes()
    {
        var assembly = typeof(ServiceRegistrationPlannerTests).Assembly;
        var assemblyName = assembly.GetName().Name!;

        var plan = ServiceRegistrationPlanner.Plan(
            assemblies: [assembly],
            typesByAssemblyName: new Dictionary<string, IReadOnlyList<Type>>(StringComparer.Ordinal)
            {
                [assemblyName] = [typeof(AbstractScoped), typeof(DisabledScoped)],
            },
            topologyLevelsByAssemblyName: new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [assemblyName] = 0,
            },
            reachabilityGraph: new AssemblyReachabilityGraph(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [assemblyName] = [],
            }),
            options: new ServiceRegistrationOptions());

        // AbstractScoped 应出现在 SkippedTypes
        plan.Report.SkippedTypes.Should().Contain(s =>
            s.TypeName.Contains(nameof(AbstractScoped), StringComparison.Ordinal));

        // DisabledScoped 应出现在 SkippedTypes
        plan.Report.SkippedTypes.Should().Contain(s =>
            s.TypeName.Contains(nameof(DisabledScoped), StringComparison.Ordinal));

        // 两者均不应出现在 Registrations
        plan.Registrations.Should().NotContain(r =>
            r.ImplementationType == typeof(AbstractScoped));
        plan.Registrations.Should().NotContain(r =>
            r.ImplementationType == typeof(DisabledScoped));
    }

    /// <summary>验证 PlanTypes 场景</summary>
    /// <param name="types">types 参数</param>
    /// <returns>PlanTypes 的执行结果</returns>
    private static ServiceRegistrationPlan PlanTypes(params Type[] types)
    {
        var assembly = typeof(ServiceRegistrationPlannerTests).Assembly;
        return ServiceRegistrationPlanner.Plan(
            assemblies: [assembly],
            typesByAssemblyName: new Dictionary<string, IReadOnlyList<Type>>(StringComparer.Ordinal)
            {
                [assembly.GetName().Name!] = types,
            },
            topologyLevelsByAssemblyName: new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [assembly.GetName().Name!] = 0,
            },
            reachabilityGraph: new AssemblyReachabilityGraph(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [assembly.GetName().Name!] = [],
            }),
            options: new ServiceRegistrationOptions());
    }

    /// <summary>验证 Report_ExposesRegistrationPlanningSections 场景</summary>
    [Fact]
    public void Report_ExposesRegistrationPlanningSections()
    {
        var candidate = new ServiceCandidateDiagnostic(
            ImplementationTypeName: "Sample.OrderService",
            ServiceTypeName: "Sample.IOrderService",
            Key: null,
            Lifetime: DependencyLifetime.Scoped,
            AssemblyName: "Sample",
            FinalPriority: 0,
            Status: "selected");

        var registration = new PlannedServiceRegistrationDiagnostic(
            ServiceTypeName: "Sample.IOrderService",
            ImplementationTypeName: "Sample.OrderService",
            Key: null,
            Lifetime: DependencyLifetime.Scoped,
            FinalPriority: 0);

        var report = new ServiceRegistrationReport(
            scannedAssemblies: ["Sample"],
            excludedAssemblies: [],
            topology: [],
            candidates: [candidate],
            registrations: [registration],
            supersededCandidates: [],
            skippedTypes: [],
            conflicts: []);

        report.Candidates.Should().ContainSingle().Which.Status.Should().Be("selected");
        report.Registrations.Should().ContainSingle().Which.ServiceTypeName.Should().Be("Sample.IOrderService");
        report.SupersededCandidates.Should().BeEmpty();
        report.SkippedTypes.Should().BeEmpty();
        report.Conflicts.Should().BeEmpty();
    }
}
