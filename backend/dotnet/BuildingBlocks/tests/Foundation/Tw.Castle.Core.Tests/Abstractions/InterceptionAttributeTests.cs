using System.Reflection;
using AwesomeAssertions;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.Castle.Core.Tests.Abstractions;

public class InterceptionAttributeTests
{
    private sealed class AuditInterceptor : IInterceptor
    {
        public ValueTask InterceptAsync(IInvocationContext context) => ValueTask.CompletedTask;
    }

    [Fact]
    public void IInterceptor_LivesIn_AbstractionsNamespace()
    {
        typeof(IInterceptor).Namespace.Should().Be("Tw.Castle.Core.Abstractions");
        typeof(IInvocationContext).Namespace.Should().Be("Tw.Castle.Core.Abstractions");
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
