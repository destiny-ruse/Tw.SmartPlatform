using AwesomeAssertions;
using Tw.Castle.Core;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.Castle.Core.Tests;

/// <summary>
/// 覆盖InterceptionRegistrationPlanner的核心行为和边界条件
/// </summary>
public class InterceptionRegistrationPlannerTests
{
    /// <summary>
    /// 定义Audited服务的能力边界
    /// </summary>
    public interface IAuditedService
    {
        /// <summary>
        /// 说明Do在当前类型中的职责
        /// </summary>
        void Do();
    }

    /// <summary>
    /// 覆盖Audited服务的核心行为和边界条件
    /// </summary>
    [Intercept(typeof(AuditInterceptor))]
    public sealed class AuditedService : IAuditedService
    {
        /// <summary>
        /// 说明Do在当前类型中的职责
        /// </summary>
        public void Do()
        {
        }
    }

    /// <summary>
    /// 覆盖审计拦截器的核心行为和边界条件
    /// </summary>
    public sealed class AuditInterceptor : IInterceptor
    {
        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>
    /// 说明Candidate在当前类型中的职责
    /// </summary>
    /// <param name="serviceType">服务注册中暴露的服务类型</param>
    /// <param name="implementationType">服务注册中使用的实现类型</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static InterceptionCandidate Candidate(Type serviceType, Type implementationType) =>
        new(serviceType, implementationType);

    /// <summary>
    /// 验证PlanCollects必需拦截器类型集合针对已选择拦截器集合
    /// </summary>
    [Fact]
    public void Plan_CollectsRequiredInterceptorTypes_ForSelectedInterceptors()
    {
        var registrations = new[] { Candidate(typeof(IAuditedService), typeof(AuditedService)) };

        var result = InterceptionRegistrationPlanner.Plan(registrations, new AttributeInterceptorSelector());

        result.RequiredInterceptorTypes.Should().BeEquivalentTo([typeof(AuditInterceptor)]);
    }

    /// <summary>
    /// 验证Plan报告Interface代理Enabled针对InterceptedInterface服务
    /// </summary>
    [Fact]
    public void Plan_ReportsInterfaceProxyEnabled_ForInterceptedInterfaceService()
    {
        var registrations = new[] { Candidate(typeof(IAuditedService), typeof(AuditedService)) };

        var result = InterceptionRegistrationPlanner.Plan(registrations, new AttributeInterceptorSelector());

        result.Report.Items.Should().Contain(item =>
            item.Carrier == "CastleInterfaceProxy" && item.Status == "enabled");
    }
}
