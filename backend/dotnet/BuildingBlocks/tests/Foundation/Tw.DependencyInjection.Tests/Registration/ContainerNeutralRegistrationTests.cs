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
    /// 验证 Tw.DependencyInjection 运行时程序集不引用已移除的容器和代理程序集
    /// </summary>
    [Fact]
    public void DependencyInjectionAssembly_DoesNotReferenceAutofacOrCastle()
    {
        typeof(ServiceRegistrationPlan).Assembly
            .GetReferencedAssemblies()
            .Select(name => name.Name)
            .Should()
            .NotContain(name =>
                name != null
                && (name.StartsWith("Autofac", StringComparison.Ordinal)
                    || name.StartsWith("Castle.", StringComparison.Ordinal)
                    || name.StartsWith("Tw.Castle.", StringComparison.Ordinal)
                    || name.StartsWith("Tw.DependencyInjection.Autofac", StringComparison.Ordinal)));
    }
}
