using AwesomeAssertions;
using Tw.Castle.Core;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.Castle.Core.Tests;

/// <summary>验证 InterceptionRegistrationPlannerTests 相关行为</summary>
public class InterceptionRegistrationPlannerTests
{
    /// <summary>定义 IAuditedService 契约</summary>
    public interface IAuditedService
    {
        /// <summary>验证 Do 场景</summary>
        void Do();
    }

    /// <summary>验证 AuditedService 相关行为</summary>
    [Intercept(typeof(AuditInterceptor))]
    public sealed class AuditedService : IAuditedService
    {
        /// <summary>验证 Do 场景</summary>
        public void Do()
        {
        }
    }

    /// <summary>验证 AuditInterceptor 相关行为</summary>
    public sealed class AuditInterceptor : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>验证 Candidate 场景</summary>
    /// <param name="serviceType">serviceType 参数</param>
    /// <param name="implementationType">implementationType 参数</param>
    /// <returns>Candidate 的执行结果</returns>
    private static InterceptionCandidate Candidate(Type serviceType, Type implementationType) =>
        new(serviceType, implementationType);

    /// <summary>验证 Plan_CollectsRequiredInterceptorTypes_ForSelectedInterceptors 场景</summary>
    [Fact]
    public void Plan_CollectsRequiredInterceptorTypes_ForSelectedInterceptors()
    {
        var registrations = new[] { Candidate(typeof(IAuditedService), typeof(AuditedService)) };

        var result = InterceptionRegistrationPlanner.Plan(registrations, new AttributeInterceptorSelector());

        result.RequiredInterceptorTypes.Should().BeEquivalentTo([typeof(AuditInterceptor)]);
    }

    /// <summary>验证 Plan_ReportsInterfaceProxyEnabled_ForInterceptedInterfaceService 场景</summary>
    [Fact]
    public void Plan_ReportsInterfaceProxyEnabled_ForInterceptedInterfaceService()
    {
        var registrations = new[] { Candidate(typeof(IAuditedService), typeof(AuditedService)) };

        var result = InterceptionRegistrationPlanner.Plan(registrations, new AttributeInterceptorSelector());

        result.Report.Items.Should().Contain(item =>
            item.Carrier == "CastleInterfaceProxy" && item.Status == "enabled");
    }
}
