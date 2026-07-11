using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.AspNetCore.Mvc;
using Tw.AspNetCore.Mvc.Context;
using Tw.Threading;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.DependencyInjection;

/// <summary>
/// 覆盖 MVC 集成服务注册扩展的核心行为和依赖边界
/// </summary>
public sealed class MvcIntegrationServiceCollectionExtensionsTests
{
    /// <summary>
    /// 验证 AddMvcIntegration 保留取消令牌提供器注册并返回同一服务集合
    /// </summary>
    [Fact]
    public void AddMvcIntegration_RegistersCancellationTokenProviderAndReturnsSameServices()
    {
        IServiceCollection services = new ServiceCollection();

        var result = services.AddMvcIntegration();

        result.Should().BeSameAs(services);
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICancellationTokenProvider>()
            .Should().BeOfType<HttpContextCancellationTokenProvider>();
    }

    /// <summary>
    /// 验证 Tw.AspNetCore.Mvc 运行时程序集不再引用已移除的容器和代理程序集
    /// </summary>
    [Fact]
    public void TwAspNetCoreMvcAssembly_DoesNotReferenceAutofacOrCastle()
    {
        var referencedAssemblyNames = typeof(MvcIntegrationServiceCollectionExtensions).Assembly
            .GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name);

        referencedAssemblyNames.Should().NotContain(name =>
            name != null
            && (name.StartsWith("Autofac", StringComparison.Ordinal)
                || name.StartsWith("Castle.", StringComparison.Ordinal)
                || name.StartsWith("Tw.Castle.", StringComparison.Ordinal)
                || name.StartsWith("Tw.DependencyInjection.Autofac", StringComparison.Ordinal)));
    }
}
