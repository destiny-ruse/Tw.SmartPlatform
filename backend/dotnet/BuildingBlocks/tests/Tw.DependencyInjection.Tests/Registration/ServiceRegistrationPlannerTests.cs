using System.Reflection;
using FluentAssertions;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Diagnostics;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

public class ServiceRegistrationPlannerTests
{
    private interface IPaymentProvider;

    private sealed class DefaultPaymentProvider : IPaymentProvider, IScopedDependency;

    [ServicePriority(10)]
    private sealed class PreferredPaymentProvider : IPaymentProvider, IScopedDependency;

    [ExposeKeyedService(typeof(IPaymentProvider), "wechat")]
    private sealed class WechatPaymentProvider : IPaymentProvider, IScopedDependency;

    [ExposeKeyedService(typeof(IPaymentProvider), "wechat")]
    [ServicePriority(5)]
    private sealed class PreferredWechatPaymentProvider : IPaymentProvider, IScopedDependency;

    [ExposeServices(typeof(IPaymentProvider))]
    private sealed class DefaultPaymentProviderClone : IPaymentProvider, IScopedDependency;

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

    [Fact]
    public void Planner_ArbitratesKeyedCandidatesPerKey()
    {
        var plan = PlanTypes(typeof(WechatPaymentProvider), typeof(PreferredWechatPaymentProvider));

        plan.Registrations.Should().ContainSingle(r =>
            r.ServiceType == typeof(IPaymentProvider)
            && r.ImplementationType == typeof(PreferredWechatPaymentProvider)
            && Equals(r.Key, "wechat"));
    }

    [Fact]
    public void Planner_ThrowsWhenFinalPriorityTies()
    {
        var act = () => PlanTypes(typeof(DefaultPaymentProvider), typeof(DefaultPaymentProviderClone));

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*最终优先级相同*");
    }

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
