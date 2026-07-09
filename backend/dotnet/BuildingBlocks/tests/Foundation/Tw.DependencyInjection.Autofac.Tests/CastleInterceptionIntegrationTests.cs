using System.Reflection;
using System.Reflection.Emit;
using Autofac;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Autofac;
using Tw.Castle.Core;
using Tw.DependencyInjection.Discovery;
using Tw.DependencyInjection.Diagnostics;
using Tw.DependencyInjection.Tests.Fixtures;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.DependencyInjection.Autofac.Tests;

/// <summary>验证 CastleInterceptionIntegrationTests 相关行为</summary>
public class CastleInterceptionIntegrationTests
{
    /// <summary>验证 AddServiceRegistration_WithInterceptedInterfaceService_UsesCastleInterfaceProxy 场景</summary>
    /// <returns>AddServiceRegistration_WithInterceptedInterfaceService_UsesCastleInterfaceProxy 的执行结果</returns>
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

    /// <summary>验证 AddServiceRegistration_BuildThrows_WhenSelectedInterceptorTypeNotRegistered 场景</summary>
    [Fact]
    public void AddServiceRegistration_BuildThrows_WhenSelectedInterceptorTypeNotRegistered()
    {
        var implementationType = DynamicAuditedOrderServiceBuilder.Build();
        var builder = new ContainerBuilder();
        // 刻意不注册 AuditInterceptor：容器构建阶段应按「拦截器类型未注册」启动失败
        builder.AddServiceRegistration(EmptyConfiguration(), new SingleAssemblySource(implementationType.Assembly));

        var act = () => builder.Build();

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage($"*{typeof(AuditInterceptor).FullName}*");
    }

    /// <summary>验证 AddServiceRegistration_WithPublicClassOnlyVirtualService_UsesCastleClassProxy 场景</summary>
    /// <returns>AddServiceRegistration_WithPublicClassOnlyVirtualService_UsesCastleClassProxy 的执行结果</returns>
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

        var report = container.Resolve<InterceptionReport>();
        report.Items.Should().Contain(item =>
            item.ServiceTypeName == implementationType.FullName
            && item.ImplementationTypeName == implementationType.FullName
            && item.MethodName == "SubmitAsync"
            && item.Carrier == "CastleClassProxy"
            && item.Status == "enabled");
        report.Items.Should().NotContain(item =>
            item.ServiceTypeName == implementationType.FullName
            && item.ImplementationTypeName == implementationType.FullName
            && item.Carrier == "CastleClassProxy"
            && (item.MethodName == nameof(ToString)
                || item.MethodName == nameof(Equals)
                || item.MethodName == nameof(GetHashCode)));
    }

    /// <summary>验证 AddServiceRegistration_WithPublicClassOnlyNonVirtualService_DoesNotReportEnabledClassProxy 场景</summary>
    [Fact]
    public void AddServiceRegistration_WithPublicClassOnlyNonVirtualService_DoesNotReportEnabledClassProxy()
    {
        var implementationType = DynamicClassOnlyServiceBuilder.Build(
            "Tw.DependencyInjection.Tests.DynamicInterceptionFixtures.NonVirtualClassOnlyOrderService",
            isPublic: true,
            isOpenGeneric: false,
            isVirtual: false);
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
            && item.Reason == "目标方法不是可重写 virtual 方法，无法使用 Castle class proxy");
        report.Items.Should().NotContain(item =>
            item.ServiceTypeName == implementationType.FullName
            && item.ImplementationTypeName == implementationType.FullName
            && item.Carrier == "CastleClassProxy"
            && item.Status == "enabled");
    }

    /// <summary>验证 AddServiceRegistration_WithOpenGenericClassOnlyService_DoesNotReportEnabledClassProxy 场景</summary>
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

    /// <summary>验证 AddServiceRegistration_WithNestedPublicClassOnlyServiceInsideNonPublicOuter_DoesNotReportEnabledClassProxy 场景</summary>
    [Fact]
    public void AddServiceRegistration_WithNestedPublicClassOnlyServiceInsideNonPublicOuter_DoesNotReportEnabledClassProxy()
    {
        var implementationType = DynamicClassOnlyServiceBuilder.BuildNestedPublicInNonPublicOuter();
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

    /// <summary>验证 AddServiceRegistration_WithNonPublicClassOnlyService_DoesNotReportEnabledClassProxy 场景</summary>
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

    /// <summary>验证 AddServiceRegistration_WithAutofacPath_PreservesKeyedServiceEntries 场景</summary>
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

    /// <summary>验证 AddServiceRegistration_WithAutofacPath_RegistersOptionsBindingReport 场景</summary>
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

    /// <summary>验证 AddServiceRegistration_ReplacesExistingNonKeyedEnumerableRegistration 场景</summary>
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

    /// <summary>验证 AddServiceRegistration_ReplacesExistingOpenGenericNonKeyedEnumerableRegistration 场景</summary>
    [Fact]
    public void AddServiceRegistration_ReplacesExistingOpenGenericNonKeyedEnumerableRegistration()
    {
        var implementationType = DynamicOpenGenericRepositoryBuilder.Build();
        var builder = new ContainerBuilder();

        builder.RegisterGeneric(typeof(OldRepository<>)).As(typeof(IRepository<>));
        builder.AddServiceRegistration(EmptyConfiguration(), new SingleAssemblySource(implementationType.Assembly));
        using var container = builder.Build();
        using var scope = container.BeginLifetimeScope();

        var service = scope.Resolve<IRepository<OrderEntity>>();
        var services = scope.Resolve<IEnumerable<IRepository<OrderEntity>>>().ToList();

        service.GetType().GetGenericTypeDefinition().Should().Be(implementationType);
        services.Should().ContainSingle();
        services[0].GetType().GetGenericTypeDefinition().Should().Be(implementationType);
        services.Should().NotContain(item => item.GetType().GetGenericTypeDefinition() == typeof(OldRepository<>));
    }

    /// <summary>验证 AddServiceRegistration_WithInheritedInterfaceMethodInterceptAttribute_UsesCastleInterfaceProxy 场景</summary>
    /// <returns>AddServiceRegistration_WithInheritedInterfaceMethodInterceptAttribute_UsesCastleInterfaceProxy 的执行结果</returns>
    [Fact]
    public async Task AddServiceRegistration_WithInheritedInterfaceMethodInterceptAttribute_UsesCastleInterfaceProxy()
    {
        var inheritedFixture = DynamicInheritedInterfaceServiceBuilder.Build(methodLevelIntercept: true);
        var recorder = new AuditRecorder();
        var builder = new ContainerBuilder();

        builder.RegisterInstance(recorder).SingleInstance();
        builder.RegisterType<AuditInterceptor>().AsSelf().InstancePerLifetimeScope();
        builder.AddServiceRegistration(EmptyConfiguration(), new SingleAssemblySource(inheritedFixture.Assembly));
        using var container = builder.Build();
        await using var scope = container.BeginLifetimeScope();

        var service = scope.Resolve(inheritedFixture.ChildInterfaceType);
        var result = await InvokeSubmitAsync(service, inheritedFixture.BaseInterfaceType, "A");

        result.Should().Be("audited:B");
        recorder.OriginalArguments.Should().Equal("A");
        recorder.TargetReturnValues.Should().Equal("B");

        var report = container.Resolve<InterceptionReport>();
        report.Items.Should().Contain(item =>
            item.ServiceTypeName == inheritedFixture.ChildInterfaceType.FullName
            && item.ImplementationTypeName == inheritedFixture.ImplementationType.FullName
            && item.MethodName == "SubmitAsync"
            && item.Carrier == "CastleInterfaceProxy"
            && item.Status == "enabled"
            && item.InterceptorTypeNames.Contains(typeof(AuditInterceptor).FullName!));
    }

    /// <summary>验证 AddServiceRegistration_WithChildInterfaceTypeInterceptAttribute_InterceptsInheritedInterfaceMethod 场景</summary>
    /// <returns>AddServiceRegistration_WithChildInterfaceTypeInterceptAttribute_InterceptsInheritedInterfaceMethod 的执行结果</returns>
    [Fact]
    public async Task AddServiceRegistration_WithChildInterfaceTypeInterceptAttribute_InterceptsInheritedInterfaceMethod()
    {
        var inheritedFixture = DynamicInheritedInterfaceServiceBuilder.Build(methodLevelIntercept: false);
        var recorder = new AuditRecorder();
        var builder = new ContainerBuilder();

        builder.RegisterInstance(recorder).SingleInstance();
        builder.RegisterType<AuditInterceptor>().AsSelf().InstancePerLifetimeScope();
        builder.AddServiceRegistration(EmptyConfiguration(), new SingleAssemblySource(inheritedFixture.Assembly));
        using var container = builder.Build();
        await using var scope = container.BeginLifetimeScope();

        var service = scope.Resolve(inheritedFixture.ChildInterfaceType);
        var result = await InvokeSubmitAsync(service, inheritedFixture.BaseInterfaceType, "A");

        result.Should().Be("audited:B");
        recorder.OriginalArguments.Should().Equal("A");
        recorder.TargetReturnValues.Should().Equal("B");
    }

    /// <summary>验证 AddServiceRegistration_WithSiblingInheritedInterfaceTypeInterceptAttribute_DoesNotApplySiblingInterceptor 场景</summary>
    /// <returns>AddServiceRegistration_WithSiblingInheritedInterfaceTypeInterceptAttribute_DoesNotApplySiblingInterceptor 的执行结果</returns>
    [Fact]
    public async Task AddServiceRegistration_WithSiblingInheritedInterfaceTypeInterceptAttribute_DoesNotApplySiblingInterceptor()
    {
        var inheritedFixture = DynamicSiblingInheritedInterfaceServiceBuilder.Build();
        var recorder = new AuditRecorder();
        var siblingRecorder = new SiblingAuditRecorder();
        var builder = new ContainerBuilder();

        builder.RegisterInstance(recorder).SingleInstance();
        builder.RegisterInstance(siblingRecorder).SingleInstance();
        builder.RegisterType<AuditInterceptor>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<SiblingAuditInterceptor>().AsSelf().InstancePerLifetimeScope();
        builder.AddServiceRegistration(EmptyConfiguration(), new SingleAssemblySource(inheritedFixture.Assembly));
        using var container = builder.Build();
        await using var scope = container.BeginLifetimeScope();

        var service = scope.Resolve(inheritedFixture.ChildInterfaceType);
        var result = await InvokeSubmitAsync(service, inheritedFixture.BaseInterfaceType, "A");

        result.Should().Be("audited:B");
        recorder.OriginalArguments.Should().Equal("A");
        recorder.TargetReturnValues.Should().Equal("B");
        siblingRecorder.OriginalArguments.Should().BeEmpty();
        siblingRecorder.TargetReturnValues.Should().BeEmpty();

        var report = container.Resolve<InterceptionReport>();
        report.Items.Should().Contain(item =>
            item.ServiceTypeName == inheritedFixture.ChildInterfaceType.FullName
            && item.ImplementationTypeName == inheritedFixture.ImplementationType.FullName
            && item.MethodName == "SubmitAsync"
            && item.Carrier == "CastleInterfaceProxy"
            && item.Status == "enabled"
            && item.InterceptorTypeNames.Contains(typeof(AuditInterceptor).FullName!));
        report.Items.Should().NotContain(item =>
            item.ServiceTypeName == inheritedFixture.SiblingInterfaceType.FullName
            && item.ImplementationTypeName == inheritedFixture.ImplementationType.FullName
            && item.Carrier == "CastleInterfaceProxy"
            && item.Status == "enabled");
    }

    /// <summary>定义 IAuditedOrderService 契约</summary>
    public interface IAuditedOrderService
    {
        /// <summary>验证 SubmitAsync 场景</summary>
        /// <param name="id">id 参数</param>
        /// <returns>SubmitAsync 的执行结果</returns>
        Task<string> SubmitAsync(string id);
    }

    /// <summary>验证 AuditRecorder 相关行为</summary>
    public sealed class AuditRecorder
    {
        /// <summary>表示 OriginalArguments 属性</summary>
        public List<string> OriginalArguments { get; } = [];

        /// <summary>表示 TargetReturnValues 属性</summary>
        public List<string> TargetReturnValues { get; } = [];
    }

    /// <summary>验证 AuditInterceptor 相关行为</summary>
    public sealed class AuditInterceptor(AuditRecorder recorder) : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            recorder.OriginalArguments.Add((string)context.Arguments[0]!);
            context.Arguments[0] = "B";

            await context.ProceedAsync();

            recorder.TargetReturnValues.Add((string)context.ReturnValue!);
            context.ReturnValue = $"audited:{context.ReturnValue}";
        }
    }

    /// <summary>验证 SiblingAuditRecorder 相关行为</summary>
    public sealed class SiblingAuditRecorder
    {
        /// <summary>表示 OriginalArguments 属性</summary>
        public List<string> OriginalArguments { get; } = [];

        /// <summary>表示 TargetReturnValues 属性</summary>
        public List<string> TargetReturnValues { get; } = [];
    }

    /// <summary>验证 SiblingAuditInterceptor 相关行为</summary>
    public sealed class SiblingAuditInterceptor(SiblingAuditRecorder recorder) : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            recorder.OriginalArguments.Add((string)context.Arguments[0]!);
            context.Arguments[0] = "S";

            await context.ProceedAsync();

            recorder.TargetReturnValues.Add((string)context.ReturnValue!);
            context.ReturnValue = $"sibling:{context.ReturnValue}";
        }
    }

    /// <summary>验证 ExistingAuditedOrderService 相关行为</summary>
    private sealed class ExistingAuditedOrderService : IAuditedOrderService
    {
        /// <summary>验证 SubmitAsync 场景</summary>
        /// <param name="id">id 参数</param>
        /// <returns>SubmitAsync 的执行结果</returns>
        public Task<string> SubmitAsync(string id) => Task.FromResult($"existing:{id}");
    }

    /// <summary>验证 OldRepository 相关行为</summary>
    /// <typeparam name="TEntity">TEntity 类型参数</typeparam>
    private sealed class OldRepository<TEntity> : IRepository<TEntity>;

    /// <summary>验证 SingleAssemblySource 相关行为</summary>
    private sealed class SingleAssemblySource(Assembly assembly) : IAssemblySource
    {
        /// <summary>验证 GetCandidateAssemblies 场景</summary>
        /// <returns>GetCandidateAssemblies 的执行结果</returns>
        public IReadOnlyList<Assembly> GetCandidateAssemblies() => [assembly];
    }

    /// <summary>验证 EmptyConfiguration 场景</summary>
    /// <returns>EmptyConfiguration 的执行结果</returns>
    private static IConfiguration EmptyConfiguration() => new ConfigurationBuilder().Build();

    /// <summary>验证 FixtureConfiguration 场景</summary>
    /// <returns>FixtureConfiguration 的执行结果</returns>
    private static IConfiguration FixtureConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IntegrationCache:Endpoint"] = "localhost",
                ["Tw:Redis:Endpoint"] = "redis",
            })
            .Build();

    /// <summary>验证 InvokeSubmitAsync 场景</summary>
    /// <param name="service">service 参数</param>
    /// <param name="serviceType">serviceType 参数</param>
    /// <param name="id">id 参数</param>
    /// <returns>InvokeSubmitAsync 的执行结果</returns>
    private static async Task<string> InvokeSubmitAsync(object service, Type serviceType, string id)
    {
        var task = (Task<string>)serviceType.GetMethod("SubmitAsync")!.Invoke(service, [id])!;
        return await task;
    }

    /// <summary>验证 DynamicAuditedOrderServiceBuilder 相关行为</summary>
    private static class DynamicAuditedOrderServiceBuilder
    {
        /// <summary>验证 Build 场景</summary>
        /// <returns>Build 的执行结果</returns>
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

    /// <summary>验证 DynamicClassOnlyServiceBuilder 相关行为</summary>
    private static class DynamicClassOnlyServiceBuilder
    {
        /// <summary>验证 Build 场景</summary>
        /// <param name="typeName">typeName 参数</param>
        /// <param name="isPublic">isPublic 参数</param>
        /// <param name="isOpenGeneric">isOpenGeneric 参数</param>
        /// <param name="isVirtual">isVirtual 参数</param>
        /// <returns>Build 的执行结果</returns>
        public static Type Build(string typeName, bool isPublic, bool isOpenGeneric, bool isVirtual = true)
        {
            var assemblyName = new AssemblyName(
                $"Tw.DependencyInjection.Tests.DynamicClassOnlyFixtures.{Guid.NewGuid():N}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
            var typeAttributes = (isPublic ? TypeAttributes.Public : TypeAttributes.NotPublic)
                | TypeAttributes.Class;
            var typeBuilder = moduleBuilder.DefineType(typeName, typeAttributes);

            DefineClassOnlyService(typeBuilder, isOpenGeneric, isVirtual);

            return typeBuilder.CreateType();
        }

        /// <summary>验证 BuildNestedPublicInNonPublicOuter 场景</summary>
        /// <returns>BuildNestedPublicInNonPublicOuter 的执行结果</returns>
        public static Type BuildNestedPublicInNonPublicOuter()
        {
            var fixtureId = Guid.NewGuid().ToString("N");
            var assemblyName = new AssemblyName($"Tw.DependencyInjection.Tests.DynamicNestedFixtures.{fixtureId}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
            var outerBuilder = moduleBuilder.DefineType(
                $"Tw.DependencyInjection.Tests.DynamicNestedFixtures.{fixtureId}.NonPublicOuter",
                TypeAttributes.NotPublic | TypeAttributes.Class);
            var nestedBuilder = outerBuilder.DefineNestedType(
                "NestedClassOnlyService",
                TypeAttributes.NestedPublic | TypeAttributes.Class);

            DefineClassOnlyService(nestedBuilder, isOpenGeneric: false, isVirtual: true);

            var nestedType = nestedBuilder.CreateType();
            outerBuilder.CreateType();
            return nestedType;
        }

        /// <summary>验证 DefineClassOnlyService 场景</summary>
        /// <param name="typeBuilder">typeBuilder 参数</param>
        /// <param name="isOpenGeneric">isOpenGeneric 参数</param>
        /// <param name="isVirtual">isVirtual 参数</param>
        private static void DefineClassOnlyService(TypeBuilder typeBuilder, bool isOpenGeneric, bool isVirtual)
        {
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
                | MethodAttributes.HideBySig
                | (isVirtual ? MethodAttributes.Virtual | MethodAttributes.NewSlot : 0),
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
        }
    }

    /// <summary>验证 DynamicOpenGenericRepositoryBuilder 相关行为</summary>
    private static class DynamicOpenGenericRepositoryBuilder
    {
        /// <summary>验证 Build 场景</summary>
        /// <returns>Build 的执行结果</returns>
        public static Type Build()
        {
            var fixtureId = Guid.NewGuid().ToString("N");
            var assemblyName = new AssemblyName($"Tw.DependencyInjection.Tests.DynamicOpenGenericFixtures.{fixtureId}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
            var typeBuilder = moduleBuilder.DefineType(
                $"Tw.DependencyInjection.Tests.DynamicOpenGenericFixtures.{fixtureId}.NewRepository`1",
                TypeAttributes.Public | TypeAttributes.Class);
            var genericParameters = typeBuilder.DefineGenericParameters("TEntity");

            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            typeBuilder.AddInterfaceImplementation(typeof(IRepository<>).MakeGenericType(genericParameters[0]));
            typeBuilder.AddInterfaceImplementation(typeof(IScopedDependency));

            return typeBuilder.CreateType();
        }
    }

    /// <summary>表示 InheritedInterfaceFixture 声明</summary>
    private sealed record InheritedInterfaceFixture(
        Assembly Assembly,
        Type BaseInterfaceType,
        Type ChildInterfaceType,
        Type ImplementationType);

    /// <summary>表示 SiblingInheritedInterfaceFixture 声明</summary>
    private sealed record SiblingInheritedInterfaceFixture(
        Assembly Assembly,
        Type BaseInterfaceType,
        Type ChildInterfaceType,
        Type SiblingInterfaceType,
        Type ImplementationType);

    /// <summary>验证 DynamicInheritedInterfaceServiceBuilder 相关行为</summary>
    private static class DynamicInheritedInterfaceServiceBuilder
    {
        /// <summary>验证 Build 场景</summary>
        /// <param name="methodLevelIntercept">methodLevelIntercept 参数</param>
        /// <returns>Build 的执行结果</returns>
        public static InheritedInterfaceFixture Build(bool methodLevelIntercept)
        {
            var fixtureId = Guid.NewGuid().ToString("N");
            var assemblyName = new AssemblyName($"Tw.DependencyInjection.Tests.DynamicInheritedInterfaceFixtures.{fixtureId}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
            var baseInterfaceBuilder = moduleBuilder.DefineType(
                $"Tw.DependencyInjection.Tests.DynamicInheritedInterfaceFixtures.{fixtureId}.IBaseInheritedOrderService",
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            var baseMethodBuilder = baseInterfaceBuilder.DefineMethod(
                "SubmitAsync",
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(Task<string>),
                [typeof(string)]);

            if (methodLevelIntercept)
            {
                baseMethodBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                    typeof(InterceptAttribute).GetConstructor([typeof(Type)])!,
                    [typeof(AuditInterceptor)]));
            }

            var baseInterfaceType = baseInterfaceBuilder.CreateType();
            var childInterfaceBuilder = moduleBuilder.DefineType(
                $"Tw.DependencyInjection.Tests.DynamicInheritedInterfaceFixtures.{fixtureId}.IChildInheritedOrderService",
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            childInterfaceBuilder.AddInterfaceImplementation(baseInterfaceType);

            if (!methodLevelIntercept)
            {
                childInterfaceBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                    typeof(InterceptAttribute).GetConstructor([typeof(Type)])!,
                    [typeof(AuditInterceptor)]));
            }

            var childInterfaceType = childInterfaceBuilder.CreateType();
            var implementationBuilder = moduleBuilder.DefineType(
                $"Tw.DependencyInjection.Tests.DynamicInheritedInterfaceFixtures.{fixtureId}.ChildInheritedOrderService",
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);

            implementationBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            implementationBuilder.AddInterfaceImplementation(childInterfaceType);
            implementationBuilder.AddInterfaceImplementation(typeof(IScopedDependency));

            var implementationMethodBuilder = implementationBuilder.DefineMethod(
                "SubmitAsync",
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

            var il = implementationMethodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, fromResultMethod);
            il.Emit(OpCodes.Ret);

            implementationBuilder.DefineMethodOverride(
                implementationMethodBuilder,
                baseInterfaceType.GetMethod("SubmitAsync")!);

            var implementationType = implementationBuilder.CreateType();
            return new InheritedInterfaceFixture(
                implementationType.Assembly,
                baseInterfaceType,
                childInterfaceType,
                implementationType);
        }
    }

    /// <summary>验证 DynamicSiblingInheritedInterfaceServiceBuilder 相关行为</summary>
    private static class DynamicSiblingInheritedInterfaceServiceBuilder
    {
        /// <summary>验证 Build 场景</summary>
        /// <returns>Build 的执行结果</returns>
        public static SiblingInheritedInterfaceFixture Build()
        {
            var fixtureId = Guid.NewGuid().ToString("N");
            var assemblyName = new AssemblyName($"Tw.DependencyInjection.Tests.DynamicSiblingInheritedInterfaceFixtures.{fixtureId}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
            var baseInterfaceBuilder = moduleBuilder.DefineType(
                $"Tw.DependencyInjection.Tests.DynamicSiblingInheritedInterfaceFixtures.{fixtureId}.IBaseInheritedOrderService",
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            baseInterfaceBuilder.DefineMethod(
                "SubmitAsync",
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(Task<string>),
                [typeof(string)]);
            var baseInterfaceType = baseInterfaceBuilder.CreateType();

            var childInterfaceBuilder = moduleBuilder.DefineType(
                $"Tw.DependencyInjection.Tests.DynamicSiblingInheritedInterfaceFixtures.{fixtureId}.IZChildInheritedOrderService",
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            childInterfaceBuilder.AddInterfaceImplementation(baseInterfaceType);
            childInterfaceBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(InterceptAttribute).GetConstructor([typeof(Type)])!,
                [typeof(AuditInterceptor)]));
            var childInterfaceType = childInterfaceBuilder.CreateType();

            var siblingInterfaceBuilder = moduleBuilder.DefineType(
                $"Tw.DependencyInjection.Tests.DynamicSiblingInheritedInterfaceFixtures.{fixtureId}.IAAnotherChildInheritedOrderService",
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            siblingInterfaceBuilder.AddInterfaceImplementation(baseInterfaceType);
            siblingInterfaceBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(InterceptAttribute).GetConstructor([typeof(Type)])!,
                [typeof(SiblingAuditInterceptor)]));
            var siblingInterfaceType = siblingInterfaceBuilder.CreateType();

            var implementationBuilder = moduleBuilder.DefineType(
                $"Tw.DependencyInjection.Tests.DynamicSiblingInheritedInterfaceFixtures.{fixtureId}.ZChildInheritedOrderService",
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);

            implementationBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            implementationBuilder.AddInterfaceImplementation(childInterfaceType);
            implementationBuilder.AddInterfaceImplementation(siblingInterfaceType);
            implementationBuilder.AddInterfaceImplementation(typeof(IScopedDependency));

            var implementationMethodBuilder = implementationBuilder.DefineMethod(
                "SubmitAsync",
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

            var il = implementationMethodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, fromResultMethod);
            il.Emit(OpCodes.Ret);

            implementationBuilder.DefineMethodOverride(
                implementationMethodBuilder,
                baseInterfaceType.GetMethod("SubmitAsync")!);

            var implementationType = implementationBuilder.CreateType();
            return new SiblingInheritedInterfaceFixture(
                implementationType.Assembly,
                baseInterfaceType,
                childInterfaceType,
                siblingInterfaceType,
                implementationType);
        }
    }
}
