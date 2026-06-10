using System.Reflection;
using System.Reflection.Emit;
using Autofac;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Diagnostics;
using Tw.DependencyInjection.Discovery;
using Tw.DependencyInjection.Tests.Fixtures;
using Tw.DynamicProxy.Abstractions;
using Xunit;

namespace Tw.DependencyInjection.Tests.DynamicProxy;

public class CastleInterceptionIntegrationTests
{
    [Fact]
    public async Task AddServiceRegistration_WithInterceptedInterfaceService_UsesCastleInterfaceProxy()
    {
        var implementationType = DynamicAuditedOrderServiceBuilder.Build();
        var recorder = new AuditRecorder();
        var builder = new ContainerBuilder();
        var configuration = new ConfigurationBuilder().Build();

        builder.RegisterInstance(recorder).SingleInstance();
        builder.RegisterType<AuditInterceptor>().AsSelf().InstancePerLifetimeScope();
        builder.AddServiceRegistration(configuration, new SingleAssemblySource(implementationType.Assembly));
        using var container = builder.Build();
        await using var scope = container.BeginLifetimeScope();

        var service = scope.Resolve<IAuditedOrderService>();
        var result = await service.SubmitAsync("A");

        result.Should().Be("audited:B");
        recorder.OriginalArguments.Should().Equal("A");
        recorder.TargetReturnValues.Should().Equal("B");

        var report = container.Resolve<InterceptionReport>();
        report.Items.Should().Contain(item =>
            item.ServiceTypeName == typeof(IAuditedOrderService).FullName
            && item.ImplementationTypeName == implementationType.FullName
            && item.MethodName == nameof(IAuditedOrderService.SubmitAsync)
            && item.Carrier == "CastleInterfaceProxy"
            && item.Status == "enabled"
            && item.InterceptorTypeNames.Contains(typeof(AuditInterceptor).FullName!));
        report.Items.Should().Contain(item =>
            item.ServiceTypeName == implementationType.FullName
            && item.ImplementationTypeName == implementationType.FullName
            && item.MethodName == nameof(IAuditedOrderService.SubmitAsync)
            && item.Carrier == "CastleClassProxy"
            && item.Status == "skipped");
    }

    [Fact]
    public async Task AddServiceRegistration_WithPublicClassOnlyVirtualService_UsesCastleClassProxy()
    {
        var implementationType = DynamicClassOnlyServiceBuilder.Build(
            "Tw.DependencyInjection.Tests.DynamicInterceptionFixtures.ClassOnlyOrderService",
            isPublic: true,
            isOpenGeneric: false);
        var recorder = new AuditRecorder();
        var builder = new ContainerBuilder();

        builder.RegisterInstance(recorder).SingleInstance();
        builder.RegisterType<AuditInterceptor>().AsSelf().InstancePerLifetimeScope();
        builder.AddServiceRegistration(EmptyConfiguration(), new SingleAssemblySource(implementationType.Assembly));
        using var container = builder.Build();
        await using var scope = container.BeginLifetimeScope();

        var service = scope.Resolve(implementationType);
        var result = await InvokeSubmitAsync(service, implementationType, "A");

        result.Should().Be("audited:B");
        recorder.OriginalArguments.Should().Equal("A");
        recorder.TargetReturnValues.Should().Equal("B");
        container.Resolve<InterceptionReport>().Items.Should().Contain(item =>
            item.ServiceTypeName == implementationType.FullName
            && item.ImplementationTypeName == implementationType.FullName
            && item.MethodName == "SubmitAsync"
            && item.Carrier == "CastleClassProxy"
            && item.Status == "enabled");
    }

    [Fact]
    public void AddServiceRegistration_WithOpenGenericClassOnlyService_DoesNotReportEnabledClassProxy()
    {
        var implementationType = DynamicClassOnlyServiceBuilder.Build(
            "Tw.DependencyInjection.Tests.DynamicInterceptionFixtures.OpenGenericClassOnlyService`1",
            isPublic: true,
            isOpenGeneric: true);
        var builder = new ContainerBuilder();

        builder.RegisterType<AuditInterceptor>().AsSelf().InstancePerLifetimeScope();
        builder.AddServiceRegistration(EmptyConfiguration(), new SingleAssemblySource(implementationType.Assembly));
        using var container = builder.Build();

        var report = container.Resolve<InterceptionReport>();
        report.Items.Should().Contain(item =>
            item.ServiceTypeName == implementationType.FullName
            && item.ImplementationTypeName == implementationType.FullName
            && item.MethodName == "SubmitAsync"
            && item.Carrier == "CastleClassProxy"
            && item.Status == "skipped"
            && item.Reason == "开放泛型 class-only 服务当前不承载 Castle class proxy");
        report.Items.Should().NotContain(item =>
            item.ServiceTypeName == implementationType.FullName
            && item.ImplementationTypeName == implementationType.FullName
            && item.Carrier == "CastleClassProxy"
            && item.Status == "enabled");
    }

    [Fact]
    public void AddServiceRegistration_WithNonPublicClassOnlyService_DoesNotReportEnabledClassProxy()
    {
        var implementationType = DynamicClassOnlyServiceBuilder.Build(
            "Tw.DependencyInjection.Tests.DynamicInterceptionFixtures.InternalClassOnlyService",
            isPublic: false,
            isOpenGeneric: false);
        var builder = new ContainerBuilder();

        builder.RegisterType<AuditInterceptor>().AsSelf().InstancePerLifetimeScope();
        builder.AddServiceRegistration(EmptyConfiguration(), new SingleAssemblySource(implementationType.Assembly));
        using var container = builder.Build();

        var report = container.Resolve<InterceptionReport>();
        report.Items.Should().Contain(item =>
            item.ServiceTypeName == implementationType.FullName
            && item.ImplementationTypeName == implementationType.FullName
            && item.MethodName == "SubmitAsync"
            && item.Carrier == "CastleClassProxy"
            && item.Status == "skipped"
            && item.Reason == "实现类型不是 public，无法使用 Castle class proxy");
        report.Items.Should().NotContain(item =>
            item.ServiceTypeName == implementationType.FullName
            && item.ImplementationTypeName == implementationType.FullName
            && item.Carrier == "CastleClassProxy"
            && item.Status == "enabled");
    }

    [Fact]
    public void AddServiceRegistration_WithAutofacPath_PreservesKeyedServiceEntries()
    {
        var builder = new ContainerBuilder();

        builder.AddServiceRegistration(FixtureConfiguration(), new SingleAssemblySource(typeof(OrderService).Assembly));
        using var container = builder.Build();
        using var scope = container.BeginLifetimeScope();

        scope.ResolveKeyed<IPaymentProvider>("wechat")
            .Should()
            .BeOfType<WechatPaymentProvider>();
        scope.Resolve<IEnumerable<KeyedServiceEntry<IPaymentProvider>>>()
            .Should()
            .ContainSingle(entry => Equals(entry.Key, "wechat") && entry.Service is WechatPaymentProvider);
    }

    [Fact]
    public void AddServiceRegistration_WithAutofacPath_RegistersOptionsBindingReport()
    {
        var builder = new ContainerBuilder();

        builder.AddServiceRegistration(FixtureConfiguration(), new SingleAssemblySource(typeof(OrderService).Assembly));
        using var container = builder.Build();

        container.Resolve<OptionsBindingReport>().Items.Should()
            .Contain(item => item.SectionPath == "IntegrationCache" && item.BindingStatus == "bound");
        container.Resolve<IOptions<IntegrationCacheOptions>>().Value.EffectiveEndpoint.Should().Be("localhost");
        container.Resolve<IOptionsMonitor<NamedRedisOptions>>().Get("primary").Endpoint.Should().Be("redis");
    }

    [Fact]
    public void AddServiceRegistration_ReplacesExistingNonKeyedEnumerableRegistration()
    {
        var implementationType = DynamicAuditedOrderServiceBuilder.Build();
        var recorder = new AuditRecorder();
        var builder = new ContainerBuilder();

        builder.RegisterInstance(recorder).SingleInstance();
        builder.RegisterType<AuditInterceptor>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<ExistingAuditedOrderService>().As<IAuditedOrderService>();
        builder.AddServiceRegistration(EmptyConfiguration(), new SingleAssemblySource(implementationType.Assembly));
        using var container = builder.Build();
        using var scope = container.BeginLifetimeScope();

        var services = scope.Resolve<IEnumerable<IAuditedOrderService>>().ToList();

        services.Should().ContainSingle();
        services[0].Should().NotBeOfType<ExistingAuditedOrderService>();
    }

    public interface IAuditedOrderService
    {
        Task<string> SubmitAsync(string id);
    }

    public sealed class AuditRecorder
    {
        public List<string> OriginalArguments { get; } = [];

        public List<string> TargetReturnValues { get; } = [];
    }

    public sealed class AuditInterceptor(AuditRecorder recorder) : IInterceptor
    {
        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            recorder.OriginalArguments.Add((string)context.Arguments[0]!);
            context.Arguments[0] = "B";

            await context.ProceedAsync();

            recorder.TargetReturnValues.Add((string)context.ReturnValue!);
            context.ReturnValue = $"audited:{context.ReturnValue}";
        }
    }

    private sealed class ExistingAuditedOrderService : IAuditedOrderService
    {
        public Task<string> SubmitAsync(string id) => Task.FromResult($"existing:{id}");
    }

    private sealed class SingleAssemblySource(Assembly assembly) : IAssemblySource
    {
        public IReadOnlyList<Assembly> GetCandidateAssemblies() => [assembly];
    }

    private static IConfiguration EmptyConfiguration() => new ConfigurationBuilder().Build();

    private static IConfiguration FixtureConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IntegrationCache:Endpoint"] = "localhost",
                ["Tw:Redis:Endpoint"] = "redis",
            })
            .Build();

    private static async Task<string> InvokeSubmitAsync(object service, Type serviceType, string id)
    {
        var task = (Task<string>)serviceType.GetMethod("SubmitAsync")!.Invoke(service, [id])!;
        return await task;
    }

    private static class DynamicAuditedOrderServiceBuilder
    {
        public static Type Build()
        {
            var fixtureId = Guid.NewGuid().ToString("N");
            var assemblyName = new AssemblyName($"Tw.DependencyInjection.Tests.DynamicInterceptionFixtures.{fixtureId}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
            var typeBuilder = moduleBuilder.DefineType(
                $"Tw.DependencyInjection.Tests.DynamicInterceptionFixtures.{fixtureId}.AuditedOrderService",
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);

            typeBuilder.AddInterfaceImplementation(typeof(IAuditedOrderService));
            typeBuilder.AddInterfaceImplementation(typeof(IScopedDependency));
            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(InterceptAttribute).GetConstructor([typeof(Type)])!,
                [typeof(AuditInterceptor)]));

            var methodBuilder = typeBuilder.DefineMethod(
                nameof(IAuditedOrderService.SubmitAsync),
                MethodAttributes.Public
                | MethodAttributes.Virtual
                | MethodAttributes.Final
                | MethodAttributes.HideBySig
                | MethodAttributes.NewSlot,
                typeof(Task<string>),
                [typeof(string)]);

            var fromResultMethod = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method => method.Name == nameof(Task.FromResult)
                    && method.IsGenericMethodDefinition
                    && method.GetParameters().Length == 1)
                .MakeGenericMethod(typeof(string));

            var il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, fromResultMethod);
            il.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(
                methodBuilder,
                typeof(IAuditedOrderService).GetMethod(nameof(IAuditedOrderService.SubmitAsync))!);

            return typeBuilder.CreateType();
        }
    }

    private static class DynamicClassOnlyServiceBuilder
    {
        public static Type Build(string typeName, bool isPublic, bool isOpenGeneric)
        {
            var assemblyName = new AssemblyName(
                $"Tw.DependencyInjection.Tests.DynamicClassOnlyFixtures.{Guid.NewGuid():N}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
            var typeAttributes = (isPublic ? TypeAttributes.Public : TypeAttributes.NotPublic)
                | TypeAttributes.Class;
            var typeBuilder = moduleBuilder.DefineType(typeName, typeAttributes);

            if (isOpenGeneric)
            {
                typeBuilder.DefineGenericParameters("TItem");
            }

            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            typeBuilder.AddInterfaceImplementation(typeof(IScopedDependency));
            typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(InterceptAttribute).GetConstructor([typeof(Type)])!,
                [typeof(AuditInterceptor)]));

            var methodBuilder = typeBuilder.DefineMethod(
                "SubmitAsync",
                MethodAttributes.Public
                | MethodAttributes.Virtual
                | MethodAttributes.HideBySig
                | MethodAttributes.NewSlot,
                typeof(Task<string>),
                [typeof(string)]);

            var fromResultMethod = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method => method.Name == nameof(Task.FromResult)
                    && method.IsGenericMethodDefinition
                    && method.GetParameters().Length == 1)
                .MakeGenericMethod(typeof(string));

            var il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, fromResultMethod);
            il.Emit(OpCodes.Ret);

            return typeBuilder.CreateType();
        }
    }
}
