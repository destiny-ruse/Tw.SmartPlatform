using FluentAssertions;
using Tw.DependencyInjection.Diagnostics;
using Xunit;

namespace Tw.DependencyInjection.Tests.DynamicProxy;

public class InterceptorPipelineTests
{
    [Fact]
    public void InterceptionReport_ExposesDiagnostics()
    {
        var item = new InterceptionDiagnostic(
            ServiceTypeName: "Sample.IOrderService",
            ImplementationTypeName: "Sample.OrderService",
            MethodName: "SubmitAsync",
            Carrier: "CastleInterfaceProxy",
            InterceptorTypeNames: ["Sample.AuditInterceptor"],
            Status: "enabled",
            Reason: null);

        var report = new InterceptionReport([item]);

        report.Items.Should().ContainSingle().Which.Should().BeSameAs(item);
    }
}
