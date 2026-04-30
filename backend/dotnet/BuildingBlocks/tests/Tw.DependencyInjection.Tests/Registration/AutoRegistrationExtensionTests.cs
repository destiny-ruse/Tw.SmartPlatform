using System.Reflection;
using System.Reflection.Emit;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tw.Core.Exceptions;
using Tw.DependencyInjection.Cancellation;
using Tw.DependencyInjection.Interception;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

/// <summary>
/// 验证自动注册公共扩展 API 的调用顺序与 Host/Autofac 组合行为
/// </summary>
public sealed class AutoRegistrationExtensionTests
{
    [Fact]
    public void UseAutoRegistration_Throws_When_AddAutoRegistration_Was_Not_Called()
    {
        var builder = new ContainerBuilder();
        var services = new ServiceCollection();

        var act = () => builder.UseAutoRegistration(services);

        act.Should().Throw<TwConfigurationException>()
            .WithMessage("*AddAutoRegistration*");
    }

    [Fact]
    public void AddAutoRegistration_Registers_Cancellation_Defaults()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddAutoRegistration(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICurrentCancellationTokenAccessor>().Should().NotBeNull();
        provider.GetRequiredService<ICancellationTokenProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddAutoRegistration_Registers_Matcher_Types_From_Configured_Assemblies()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddAutoRegistration(
            configuration,
            options => options.AddAssemblyOf<AssemblyMatcher>());

        using var provider = services.BuildServiceProvider();
        provider.GetServices<IInterceptorMatcher>()
            .Should().Contain(m => m.GetType() == typeof(AssemblyMatcher));
    }

    [Fact]
    public void AutoRegistrationOptions_Stores_Assemblies_And_Global_Interceptors()
    {
        var options = new AutoRegistrationOptions()
            .AddAssemblyOf<AutoRegistrationExtensionTests>()
            .EnableOptionsAutoRegistration()
            .AddGlobalInterceptor<AssemblyInterceptor>(InterceptorScope.Service, order: 7);

        options.Assemblies.Should().Contain(typeof(AutoRegistrationExtensionTests).Assembly);
        options.OptionsAutoRegistrationEnabled.Should().BeTrue();
        options.GlobalInterceptors.Should().ContainSingle()
            .Which.Should().Be(new InterceptorRegistration(typeof(AssemblyInterceptor), InterceptorScope.Service, 7));
    }

    [Fact]
    public void Host_Resolves_Scoped_Service_From_AutoRegistration()
    {
        var dynamicService = DynamicServiceAssembly.Create();
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddAutoRegistration(
            builder.Configuration,
            options => options.AddAssembly(dynamicService.Assembly));
        builder.ConfigureContainer(
            new AutofacServiceProviderFactory(),
            containerBuilder => containerBuilder.UseAutoRegistration(builder.Services));

        using var host = builder.Build();
        using var firstScope = host.Services.CreateScope();
        using var secondScope = host.Services.CreateScope();

        var first = firstScope.ServiceProvider.GetRequiredService(dynamicService.ServiceType);
        first.Should().NotBeNull();
        firstScope.ServiceProvider.GetRequiredService(dynamicService.ServiceType).Should().BeSameAs(first);
        secondScope.ServiceProvider.GetRequiredService(dynamicService.ServiceType).Should().NotBeSameAs(first);
    }

    [Fact]
    public void UseAutoRegistration_Emits_Development_Diagnostics()
    {
        var diagnosticAssembly = DiagnosticServiceAssembly.Create();
        var diagnostics = new List<string>();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Development",
            })
            .Build();

        services.AddAutoRegistration(
            configuration,
            options => options
                .AddAssembly(diagnosticAssembly.Assembly)
                .AddGlobalInterceptor<AssemblyInterceptor>(InterceptorScope.Service)
                .WriteDiagnosticsTo(diagnostics.Add));

        var containerBuilder = new ContainerBuilder();
        containerBuilder.UseAutoRegistration(services);
        using var container = containerBuilder.Build();

        diagnostics.Should().Contain(message => Contains(message, "scan") && Contains(message, "elapsed"));
        diagnostics.Should().Contain(message => Contains(message, "sort") && Contains(message, "serviceCount=1"));
        diagnostics.Should().Contain(message => Contains(message, "AOP") && Contains(message, "interceptedCount=1"));
        diagnostics.Should().Contain(message => Contains(message, "register") && Contains(message, "serviceCount=1"));
        diagnostics.Should().Contain(message => Contains(message, "total") && Contains(message, "elapsed"));
        diagnostics.Should().Contain(message => Contains(message, "warning") && Contains(message, diagnosticAssembly.ServiceType.Name));
    }

    [Fact]
    public void UseAutoRegistration_Suppresses_Diagnostics_In_Production()
    {
        var diagnosticAssembly = DiagnosticServiceAssembly.Create();
        var diagnostics = new List<string>();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Production",
            })
            .Build();

        services.AddAutoRegistration(
            configuration,
            options => options
                .AddAssembly(diagnosticAssembly.Assembly)
                .AddGlobalInterceptor<AssemblyInterceptor>(InterceptorScope.Service)
                .WriteDiagnosticsTo(diagnostics.Add));

        var containerBuilder = new ContainerBuilder();
        containerBuilder.UseAutoRegistration(services);
        using var container = containerBuilder.Build();

        diagnostics.Should().BeEmpty();
    }

    public sealed class AssemblyMatcher : IInterceptorMatcher
    {
        public Type InterceptorType => typeof(AssemblyInterceptor);

        public InterceptorScope Scope => InterceptorScope.Service;
    }

    public sealed class AssemblyInterceptor : InterceptorBase { }

    private sealed record DynamicServiceAssembly(Assembly Assembly, Type ServiceType)
    {
        public static DynamicServiceAssembly Create()
        {
            var assemblyName = new AssemblyName($"Tw.DynamicAutoRegistrationTests.{Guid.NewGuid():N}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);

            var serviceInterface = moduleBuilder
                .DefineType(
                    "ITestAutoRegisteredService",
                    TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract)
                .CreateTypeInfo()!
                .AsType();

            var implementation = moduleBuilder.DefineType(
                "TestAutoRegisteredService",
                TypeAttributes.Public | TypeAttributes.Class);
            implementation.AddInterfaceImplementation(serviceInterface);
            implementation.AddInterfaceImplementation(typeof(IScopedDependency));
            implementation.DefineDefaultConstructor(MethodAttributes.Public);
            implementation.CreateTypeInfo();

            return new DynamicServiceAssembly(assemblyBuilder, serviceInterface);
        }
    }

    private sealed record DiagnosticServiceAssembly(Assembly Assembly, Type ServiceType)
    {
        public static DiagnosticServiceAssembly Create()
        {
            var assemblyName = new AssemblyName($"Tw.DynamicAutoRegistrationDiagnostics.{Guid.NewGuid():N}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);

            var implementation = moduleBuilder.DefineType(
                "ConcreteDiagnosticService",
                TypeAttributes.Public | TypeAttributes.Class);
            implementation.AddInterfaceImplementation(typeof(IScopedDependency));
            implementation.DefineDefaultConstructor(MethodAttributes.Public);
            var implementationType = implementation.CreateTypeInfo()!.AsType();

            return new DiagnosticServiceAssembly(assemblyBuilder, implementationType);
        }
    }

    private static bool Contains(string message, string value)
        => message.Contains(value, StringComparison.OrdinalIgnoreCase);
}
