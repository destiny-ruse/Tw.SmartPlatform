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

/// <summary>验证 HostStartupBuilderExtensionsTests 相关行为</summary>
public class HostStartupBuilderExtensionsTests
{
    /// <summary>验证 UseWebIntegration_ConfiguresAutofacAndServiceRegistration 场景</summary>
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

    /// <summary>验证 UseWebIntegration_ReturnsSameBuilder_ForChaining 场景</summary>
    [Fact]
    public void UseWebIntegration_ReturnsSameBuilder_ForChaining()
    {
        var builder = CreateBuilderWithTestAssembly();

        var result = builder.UseWebIntegration();

        result.Should().BeSameAs(builder);
    }

    /// <summary>验证 UseWebIntegration_Throws_WhenBuilderIsNull 场景</summary>
    [Fact]
    public void UseWebIntegration_Throws_WhenBuilderIsNull()
    {
        WebApplicationBuilder builder = null!;

        var act = () => builder.UseWebIntegration();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("builder");
    }

    /// <summary>验证 CreateBuilderWithTestAssembly 场景</summary>
    /// <returns>CreateBuilderWithTestAssembly 的执行结果</returns>
    private static WebApplicationBuilder CreateBuilderWithTestAssembly()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tw:DependencyInjection:IncludeAssemblies:0"] = Assembly.GetExecutingAssembly().GetName().Name,
        });

        return builder;
    }

    /// <summary>定义 IHostStartupSampleService 契约</summary>
    public interface IHostStartupSampleService
    {
        /// <summary>验证 Execute 场景</summary>
        [Intercept(typeof(HostStartupSampleInterceptor))]
        void Execute();
    }

    /// <summary>验证 HostStartupSampleService 相关行为</summary>
    public sealed class HostStartupSampleService : IHostStartupSampleService, IScopedDependency
    {
        /// <summary>验证 Execute 场景</summary>
        public void Execute()
        {
        }
    }

    /// <summary>验证 HostStartupSampleInterceptor 相关行为</summary>
    public sealed class HostStartupSampleInterceptor : SyncInterceptorBase, ITransientDependency
    {
        /// <summary>表示 _invocationCount 字段</summary>
        private static int _invocationCount;

        /// <summary>表示 InvocationCount 属性</summary>
        public static int InvocationCount => Volatile.Read(ref _invocationCount);

        /// <summary>验证 Reset 场景</summary>
        public static void Reset() => Interlocked.Exchange(ref _invocationCount, 0);

        /// <summary>验证 Before 场景</summary>
        /// <param name="context">context 参数</param>
        protected override void Before(IInvocationContext context)
        {
            Interlocked.Increment(ref _invocationCount);
        }
    }
}
