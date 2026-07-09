using AwesomeAssertions;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

/// <summary>验证 ContainerNeutralRegistrationTests 相关行为</summary>
public sealed class ContainerNeutralRegistrationTests
{
    /// <summary>验证 ServiceRegistrationPlan_DoesNotExposeAutofacTypes 场景</summary>
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
