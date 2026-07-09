using System.Reflection;
using AwesomeAssertions;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.Castle.Core.Tests.Abstractions;

/// <summary>验证 InterceptionAttributeTests 相关行为</summary>
public class InterceptionAttributeTests
{
    /// <summary>验证 AuditInterceptor 相关行为</summary>
    private sealed class AuditInterceptor : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => ValueTask.CompletedTask;
    }

    /// <summary>验证 IInterceptor_LivesIn_AbstractionsNamespace 场景</summary>
    [Fact]
    public void IInterceptor_LivesIn_AbstractionsNamespace()
    {
        typeof(IInterceptor).Namespace.Should().Be("Tw.Castle.Core.Abstractions");
        typeof(IInvocationContext).Namespace.Should().Be("Tw.Castle.Core.Abstractions");
    }

    /// <summary>验证 Intercept_CarriesInterceptorType 场景</summary>
    [Fact]
    public void Intercept_CarriesInterceptorType()
    {
        var attr = new InterceptAttribute(typeof(AuditInterceptor));
        attr.InterceptorType.Should().Be(typeof(AuditInterceptor));
    }

    /// <summary>验证 Intercept_TargetsClassInterfaceMethod_AndAllowsMultiple 场景</summary>
    [Fact]
    public void Intercept_TargetsClassInterfaceMethod_AndAllowsMultiple()
    {
        var usage = typeof(InterceptAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        usage.ValidOn.Should().Be(
            AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method);
        usage.AllowMultiple.Should().BeTrue();
    }

    /// <summary>验证 DisableInterception_TargetsClassAndMethod 场景</summary>
    [Fact]
    public void DisableInterception_TargetsClassAndMethod()
    {
        var usage = typeof(DisableInterceptionAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        usage.ValidOn.Should().Be(AttributeTargets.Class | AttributeTargets.Method);
    }

    /// <summary>验证 InterceptorOrder_CarriesOrder 场景</summary>
    [Fact]
    public void InterceptorOrder_CarriesOrder()
    {
        new InterceptorOrderAttribute(5).Order.Should().Be(5);
    }
}
