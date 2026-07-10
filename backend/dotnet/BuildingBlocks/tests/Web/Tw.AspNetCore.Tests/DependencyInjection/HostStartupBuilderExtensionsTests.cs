using System.Reflection;
using Autofac.Extensions.DependencyInjection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Diagnostics;
using Tw.Castle.Core;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.AspNetCore.Tests.DependencyInjection;

/// <summary>
/// 覆盖主机启动构建器Extensions的核心行为和边界条件
/// </summary>
public class HostStartupBuilderExtensionsTests
{
    /// <summary>
    /// 验证UseWebIntegrationConfiguresAutofac和服务Registration
    /// </summary>
    [Fact]
    public void UseWebIntegration_ConfiguresAutofacAndServiceRegistration()
    {
        HostStartupSampleInterceptor.Reset();
        var builder = CreateBuilderWithTestAssembly();

        builder.UseWebIntegration();

        using var app = builder.Build();
        app.Services.Should().BeOfType<AutofacServiceProvider>();

        var registrationReport = app.Services.GetRequiredService<ServiceRegistrationReport>();
        registrationReport.Registrations.Should().Contain(registration =>
            registration.ServiceTypeName == typeof(IHostStartupSampleService).FullName
            && registration.ImplementationTypeName == typeof(HostStartupSampleService).FullName);

        var interceptionReport = app.Services.GetRequiredService<InterceptionReport>();
        interceptionReport.Items.Should().Contain(item =>
            item.ServiceTypeName == typeof(IHostStartupSampleService).FullName
            && item.ImplementationTypeName == typeof(HostStartupSampleService).FullName
            && item.MethodName == nameof(IHostStartupSampleService.Execute)
            && item.Carrier == "CastleInterfaceProxy"
            && item.Status == "enabled");

        var service = app.Services.GetRequiredService<IHostStartupSampleService>();
        service.Execute();

        HostStartupSampleInterceptor.InvocationCount.Should().Be(1);
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
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        [Intercept(typeof(HostStartupSampleInterceptor))]
        void Execute();
    }

    /// <summary>
    /// 覆盖主机启动示例服务的核心行为和边界条件
    /// </summary>
    public sealed class HostStartupSampleService : IHostStartupSampleService, IScopedDependency
    {
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        public void Execute()
        {
        }
    }

    /// <summary>
    /// 覆盖主机启动示例拦截器的核心行为和边界条件
    /// </summary>
    public sealed class HostStartupSampleInterceptor : SyncInterceptorBase, ITransientDependency
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的调用数量
        /// </summary>
        private static int _invocationCount;

        /// <summary>
        /// Read在当前对象中的业务含义
        /// </summary>
        public static int InvocationCount => Volatile.Read(ref _invocationCount);

        /// <summary>
        /// 清空测试替身记录的调用状态
        /// </summary>
        public static void Reset() => Interlocked.Exchange(ref _invocationCount, 0);

        /// <summary>
        /// 在目标调用前运行拦截器逻辑
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        protected override void Before(IInvocationContext context)
        {
            Interlocked.Increment(ref _invocationCount);
        }
    }
}
