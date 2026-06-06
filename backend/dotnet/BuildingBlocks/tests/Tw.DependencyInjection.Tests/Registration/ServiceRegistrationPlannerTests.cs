using FluentAssertions;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Diagnostics;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

public class ServiceRegistrationPlannerTests
{
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
