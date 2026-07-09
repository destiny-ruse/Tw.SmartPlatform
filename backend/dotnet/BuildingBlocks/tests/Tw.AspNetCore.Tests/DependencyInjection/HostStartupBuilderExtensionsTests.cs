using System.Reflection;
using Autofac.Extensions.DependencyInjection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tw.DependencyInjection.Abstractions;
using Tw.Castle.Core;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.AspNetCore.Tests.DependencyInjection;

public class HostStartupBuilderExtensionsTests
{
    [Fact]
    public void UseTwHostStartup_ConfiguresAutofacAndServiceRegistration()
    {
        HostStartupSampleInterceptor.Reset();
        var builder = CreateBuilderWithTestAssembly();

        builder.UseTwHostStartup();

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

    [Fact]
    public void UseTwHostStartup_ReturnsSameBuilder_ForChaining()
    {
        var builder = CreateBuilderWithTestAssembly();

        var result = builder.UseTwHostStartup();

        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void UseTwHostStartup_Throws_WhenBuilderIsNull()
    {
        WebApplicationBuilder builder = null!;

        var act = () => builder.UseTwHostStartup();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("builder");
    }

    private static WebApplicationBuilder CreateBuilderWithTestAssembly()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tw:DependencyInjection:IncludeAssemblies:0"] = Assembly.GetExecutingAssembly().GetName().Name,
        });

        return builder;
    }

    public interface IHostStartupSampleService
    {
        [Intercept(typeof(HostStartupSampleInterceptor))]
        void Execute();
    }

    public sealed class HostStartupSampleService : IHostStartupSampleService, IScopedDependency
    {
        public void Execute()
        {
        }
    }

    public sealed class HostStartupSampleInterceptor : SyncInterceptorBase, ITransientDependency
    {
        private static int _invocationCount;

        public static int InvocationCount => Volatile.Read(ref _invocationCount);

        public static void Reset() => Interlocked.Exchange(ref _invocationCount, 0);

        protected override void Before(IInvocationContext context)
        {
            Interlocked.Increment(ref _invocationCount);
        }
    }
}
