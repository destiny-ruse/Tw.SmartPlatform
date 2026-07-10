using System.Reflection;
using AwesomeAssertions;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.Castle.Core.Tests.Abstractions;

/// <summary>
/// 覆盖Interception特性的核心行为和边界条件
/// </summary>
public class InterceptionAttributeTests
{
    /// <summary>
    /// 覆盖审计拦截器的核心行为和边界条件
    /// </summary>
    private sealed class AuditInterceptor : IInterceptor
    {
        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => ValueTask.CompletedTask;
    }

    /// <summary>
    /// 验证拦截器LivesInAbstractionsNamespace
    /// </summary>
    [Fact]
    public void IInterceptor_LivesIn_AbstractionsNamespace()
    {
        typeof(IInterceptor).Namespace.Should().Be("Tw.Castle.Core.Abstractions");
        typeof(IInvocationContext).Namespace.Should().Be("Tw.Castle.Core.Abstractions");
    }

    /// <summary>
    /// 验证nterceptCarries拦截器类型
    /// </summary>
    [Fact]
    public void Intercept_CarriesInterceptorType()
    {
        var attr = new InterceptAttribute(typeof(AuditInterceptor));
        attr.InterceptorType.Should().Be(typeof(AuditInterceptor));
    }

    /// <summary>
    /// 验证nterceptTargetsClassInterface方法和AllowsMultiple
    /// </summary>
    [Fact]
    public void Intercept_TargetsClassInterfaceMethod_AndAllowsMultiple()
    {
        var usage = typeof(InterceptAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        usage.ValidOn.Should().Be(
            AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method);
        usage.AllowMultiple.Should().BeTrue();
    }

    /// <summary>
    /// 验证DisableInterceptionTargetsClass和方法
    /// </summary>
    [Fact]
    public void DisableInterception_TargetsClassAndMethod()
    {
        var usage = typeof(DisableInterceptionAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        usage.ValidOn.Should().Be(AttributeTargets.Class | AttributeTargets.Method);
    }

    /// <summary>
    /// 验证nterceptorOrderCarriesOrder
    /// </summary>
    [Fact]
    public void InterceptorOrder_CarriesOrder()
    {
        new InterceptorOrderAttribute(5).Order.Should().Be(5);
    }
}
