using AwesomeAssertions;
using Tw.Castle.Core;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.Castle.Core.Tests;

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

    private static InterceptionCandidate Candidate(Type serviceType, Type implementationType) =>
        new(serviceType, implementationType);

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
