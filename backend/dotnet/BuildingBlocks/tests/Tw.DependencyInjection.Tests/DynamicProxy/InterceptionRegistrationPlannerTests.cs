using AwesomeAssertions;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.DynamicProxy;
using Tw.DependencyInjection.Registration;
using Tw.DynamicProxy.Abstractions;
using Xunit;

namespace Tw.DependencyInjection.Tests.DynamicProxy;

public class InterceptionRegistrationPlannerTests
{
    public interface IAuditedService
    {
        void Do();
    }

    [Intercept(typeof(AuditInterceptor))]
    public sealed class AuditedService : IAuditedService
    {
        public void Do()
        {
        }
    }

    public sealed class AuditInterceptor : IInterceptor
    {
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    private static ServiceCandidate Candidate(Type serviceType, Type implementationType) =>
        new(
            serviceType,
            implementationType,
            Key: null,
            DependencyLifetime.Scoped,
            AssemblyName: "Tw.DependencyInjection.Tests",
            TopologyLevel: 0,
            AssemblyPriority: 0,
            TypePriority: 0,
            FinalPriority: 0,
            DiscoveryOrder: 0);

    [Fact]
    public void Plan_CollectsRequiredInterceptorTypes_ForSelectedInterceptors()
    {
        var registrations = new[] { Candidate(typeof(IAuditedService), typeof(AuditedService)) };

        var result = InterceptionRegistrationPlanner.Plan(registrations, new AttributeInterceptorSelector());

        result.RequiredInterceptorTypes.Should().BeEquivalentTo([typeof(AuditInterceptor)]);
    }

    [Fact]
    public void Plan_ReportsInterfaceProxyEnabled_ForInterceptedInterfaceService()
    {
        var registrations = new[] { Candidate(typeof(IAuditedService), typeof(AuditedService)) };

        var result = InterceptionRegistrationPlanner.Plan(registrations, new AttributeInterceptorSelector());

        result.Report.Items.Should().Contain(item =>
            item.Carrier == "CastleInterfaceProxy" && item.Status == "enabled");
    }
}
