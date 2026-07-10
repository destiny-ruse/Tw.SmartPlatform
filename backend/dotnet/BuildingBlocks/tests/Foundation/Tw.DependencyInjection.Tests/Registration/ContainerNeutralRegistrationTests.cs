using AwesomeAssertions;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

/// <summary>
/// 覆盖ContainerNeutralRegistration的核心行为和边界条件
/// </summary>
public sealed class ContainerNeutralRegistrationTests
{
    /// <summary>
    /// 验证服务RegistrationPlan不ExposeAutofac类型集合
    /// </summary>
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
