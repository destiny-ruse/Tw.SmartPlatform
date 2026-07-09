using AwesomeAssertions;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

public sealed class ContainerNeutralRegistrationTests
{
    [Fact]
    public void ServiceRegistrationPlan_DoesNotExposeAutofacTypes()
    {
        typeof(ServiceRegistrationPlan).Assembly
            .GetReferencedAssemblies()
            .Select(name => name.Name)
            .Should()
            .NotContain("Autofac");
    }
}
