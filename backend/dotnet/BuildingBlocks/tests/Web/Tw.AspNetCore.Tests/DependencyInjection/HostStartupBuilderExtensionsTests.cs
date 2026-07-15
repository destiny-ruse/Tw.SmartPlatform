using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Diagnostics;
using Xunit;

namespace Tw.AspNetCore.Tests.DependencyInjection;

/// <summary>
/// 覆盖主机启动构建器Extensions的核心行为和边界条件
/// </summary>
public class HostStartupBuilderExtensionsTests
{
    /// <summary>
    /// 验证UseWebIntegration通过 Microsoft DI 注册并解析服务
    /// </summary>
    [Fact]
    public void UseWebIntegration_RegistersAndResolvesServicesThroughMicrosoftDependencyInjection()
    {
        var builder = CreateBuilderWithTestAssembly();

        builder.UseWebIntegration();

        using var app = builder.Build();
        app.Services.Should().BeOfType<ServiceProvider>();

        var registrationReport = app.Services.GetRequiredService<ServiceRegistrationReport>();
        registrationReport.Registrations.Should().Contain(registration =>
            registration.ServiceTypeName == typeof(IHostStartupSampleService).FullName
            && registration.ImplementationTypeName == typeof(HostStartupSampleService).FullName);

        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IHostStartupSampleService>()
            .Should().BeOfType<HostStartupSampleService>();
    }

    /// <summary>
    /// 验证UseWebIntegration返回Same构建器针对Chaining
    /// </summary>
    [Fact]
    public void UseWebIntegration_ReturnsSameBuilder_ForChaining()
    {
        var builder = CreateBuilderWithTestAssembly();

        var result = builder.UseWebIntegration();

        result.Should().BeSameAs(builder);
    }

    /// <summary>
    /// 验证UseWebIntegration抛出异常当构建器Is空值
    /// </summary>
    [Fact]
    public void UseWebIntegration_Throws_WhenBuilderIsNull()
    {
        WebApplicationBuilder builder = null!;

        var act = () => builder.UseWebIntegration();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("builder");
    }

    /// <summary>
    /// 验证 Tw.AspNetCore 运行时程序集不再引用已移除的容器和代理程序集
    /// </summary>
    [Fact]
    public void AspNetCoreAssembly_DoesNotReferenceAutofacOrCastle()
    {
        var referencedAssemblyNames = typeof(HostStartupBuilderExtensions).Assembly
            .GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name);

        referencedAssemblyNames.Should().NotContain(name =>
            name != null
            && (name.StartsWith("Autofac", StringComparison.Ordinal)
                || name.StartsWith("Castle.", StringComparison.Ordinal)
                || name.StartsWith("Tw.Castle.", StringComparison.Ordinal)
                || name.StartsWith("Tw.DependencyInjection.Autofac", StringComparison.Ordinal)));
    }

    /// <summary>
    /// 创建构建器带有TestAssembly测试对象
    /// </summary>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static WebApplicationBuilder CreateBuilderWithTestAssembly()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tw:DependencyInjection:IncludeAssemblies:0"] = Assembly.GetExecutingAssembly().GetName().Name,
        });

        return builder;
    }

    /// <summary>
    /// 定义主机启动示例服务的能力边界
    /// </summary>
    public interface IHostStartupSampleService
    {
    }

    /// <summary>
    /// 覆盖主机启动示例服务的核心行为和边界条件
    /// </summary>
    public sealed class HostStartupSampleService : IHostStartupSampleService, IScopedDependency
    {
    }
}
