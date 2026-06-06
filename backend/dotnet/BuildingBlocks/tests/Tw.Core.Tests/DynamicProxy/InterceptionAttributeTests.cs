using System.Reflection;
using FluentAssertions;
using Tw.DynamicProxy.Abstractions;
using Xunit;

namespace Tw.Core.Tests.DynamicProxy;

public class InterceptionAttributeTests
{
    private sealed class AuditInterceptor;

    [Fact]
    public void IInterceptor_LivesIn_AbstractionsNamespace()
    {
        typeof(IInterceptor).Namespace.Should().Be("Tw.DynamicProxy.Abstractions");
        typeof(IInvocationContext).Namespace.Should().Be("Tw.DynamicProxy.Abstractions");
    }

    [Fact]
    public void Intercept_CarriesInterceptorType()
    {
        var attr = new InterceptAttribute(typeof(AuditInterceptor));
        attr.InterceptorType.Should().Be(typeof(AuditInterceptor));
    }

    [Fact]
    public void Intercept_TargetsClassInterfaceMethod_AndAllowsMultiple()
    {
        var usage = typeof(InterceptAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        usage.ValidOn.Should().Be(
            AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method);
        usage.AllowMultiple.Should().BeTrue();
    }

    [Fact]
    public void DisableInterception_TargetsClassAndMethod()
    {
        var usage = typeof(DisableInterceptionAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        usage.ValidOn.Should().Be(AttributeTargets.Class | AttributeTargets.Method);
    }

    [Fact]
    public void InterceptorOrder_CarriesOrder()
    {
        new InterceptorOrderAttribute(5).Order.Should().Be(5);
    }
}
