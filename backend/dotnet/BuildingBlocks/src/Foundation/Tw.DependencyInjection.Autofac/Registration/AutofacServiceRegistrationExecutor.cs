using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.DynamicProxy;
using Tw.DependencyInjection.Abstractions;
using Tw.Castle.Core;
using Tw.DependencyInjection.Registration;

namespace Tw.DependencyInjection.Autofac.Registration;

/// <summary>
/// 将 <see cref="ServiceRegistrationPlan"/> 中的候选项执行写入 Autofac <see cref="ContainerBuilder"/>
/// </summary>
internal static class AutofacServiceRegistrationExecutor
{
    /// <summary>表示 AddNonKeyedEnumerableMethod 字段</summary>
    private static readonly MethodInfo AddNonKeyedEnumerableMethod = typeof(AutofacServiceRegistrationExecutor)
        .GetMethod(nameof(AddNonKeyedEnumerableCore), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>表示 Enabled 常量</summary>
    private const string Enabled = "enabled";
    /// <summary>表示 CastleInterfaceProxy 常量</summary>
    private const string CastleInterfaceProxy = "CastleInterfaceProxy";
    /// <summary>表示 CastleClassProxy 常量</summary>
    private const string CastleClassProxy = "CastleClassProxy";

    /// <summary>
    /// 将服务注册规划与拦截承载报告应用到 Autofac 容器构建器
    /// </summary>
    /// <param name="builder">目标 Autofac 容器构建器</param>
    /// <param name="plan">服务注册规划结果</param>
    /// <param name="report">AOP 拦截承载诊断报告</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/>、<paramref name="plan"/> 或 <paramref name="report"/> 为 null 时抛出</exception>
    public static void Apply(ContainerBuilder builder, ServiceRegistrationPlan plan, InterceptionReport report)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(report);

        RegisterDynamicProxyServices(builder, report);
        builder.RegisterInstance(plan.Report)
            .AsSelf()
            .SingleInstance();

        foreach (var registration in plan.Registrations)
        {
            AddService(builder, registration, report);
            if (registration.Key is not null)
            {
                AddKeyedEntry(builder, registration);
            }
            else
            {
                AddNonKeyedEnumerable(builder, registration);
            }
        }

        AddNonKeyedOpenGenericEnumerableSource(builder, plan.Registrations);
    }

    /// <summary>执行 RegisterDynamicProxyServices 操作</summary>
    /// <param name="builder">builder 参数</param>
    /// <param name="report">report 参数</param>
    private static void RegisterDynamicProxyServices(ContainerBuilder builder, InterceptionReport report)
    {
        builder.RegisterType<AttributeInterceptorSelector>()
            .As<IInterceptorSelector>()
            .SingleInstance();

        builder.RegisterType<InterceptorPipeline>()
            .As<IInterceptorPipeline>()
            .SingleInstance();

        builder.Register(context => new CastleAsyncInterceptorAdapter(
                context.Resolve<IInterceptorSelector>(),
                context.Resolve<IInterceptorPipeline>(),
                new AutofacServiceProvider(context.Resolve<ILifetimeScope>())))
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterInstance(report)
            .AsSelf()
            .SingleInstance();
    }

    /// <summary>执行 AddService 操作</summary>
    /// <param name="builder">builder 参数</param>
    /// <param name="registration">registration 参数</param>
    /// <param name="report">report 参数</param>
    private static void AddService(
        ContainerBuilder builder,
        ServiceCandidate registration,
        InterceptionReport report)
    {
        if (registration.Key is null)
        {
            AddNonKeyed(builder, registration, report);
            return;
        }

        AddKeyed(builder, registration, report);
    }

    /// <summary>执行 AddNonKeyed 操作</summary>
    /// <param name="builder">builder 参数</param>
    /// <param name="registration">registration 参数</param>
    /// <param name="report">report 参数</param>
    private static void AddNonKeyed(
        ContainerBuilder builder,
        ServiceCandidate registration,
        InterceptionReport report)
    {
        if (registration.ImplementationType.IsGenericTypeDefinition)
        {
            var registrationBuilder = builder.RegisterGeneric(registration.ImplementationType)
                .As(registration.ServiceType);

            ApplyInterfaceInterception(registrationBuilder, registration, report);
            ApplyLifetime(registrationBuilder, registration.Lifetime);
            return;
        }

        var typedRegistrationBuilder = builder.RegisterType(registration.ImplementationType)
            .As(registration.ServiceType);

        ApplyTypedInterception(typedRegistrationBuilder, registration, report);
        ApplyLifetime(typedRegistrationBuilder, registration.Lifetime);
    }

    /// <summary>执行 AddKeyed 操作</summary>
    /// <param name="builder">builder 参数</param>
    /// <param name="registration">registration 参数</param>
    /// <param name="report">report 参数</param>
    private static void AddKeyed(
        ContainerBuilder builder,
        ServiceCandidate registration,
        InterceptionReport report)
    {
        if (registration.ImplementationType.IsGenericTypeDefinition)
        {
            var registrationBuilder = builder.RegisterGeneric(registration.ImplementationType)
                .Keyed(registration.Key!, registration.ServiceType);

            ApplyInterfaceInterception(registrationBuilder, registration, report);
            ApplyLifetime(registrationBuilder, registration.Lifetime);
            return;
        }

        var typedRegistrationBuilder = builder.RegisterType(registration.ImplementationType)
            .Keyed(registration.Key!, registration.ServiceType);

        ApplyTypedInterception(typedRegistrationBuilder, registration, report);
        ApplyLifetime(typedRegistrationBuilder, registration.Lifetime);
    }

    /// <summary>执行 AddKeyedEntry 操作</summary>
    /// <param name="builder">builder 参数</param>
    /// <param name="registration">registration 参数</param>
    private static void AddKeyedEntry(ContainerBuilder builder, ServiceCandidate registration)
    {
        if (registration.ServiceType.IsGenericTypeDefinition)
        {
            return;
        }

        var entryType = typeof(KeyedServiceEntry<>).MakeGenericType(registration.ServiceType);
        var registrationBuilder = builder.Register(context =>
            {
                var service = context.ResolveKeyed(registration.Key!, registration.ServiceType);
                return Activator.CreateInstance(entryType, registration.Key!, service)!;
            })
            .As(entryType);

        ApplyLifetime(registrationBuilder, registration.Lifetime);
    }

    /// <summary>执行 AddNonKeyedEnumerable 操作</summary>
    /// <param name="builder">builder 参数</param>
    /// <param name="registration">registration 参数</param>
    private static void AddNonKeyedEnumerable(ContainerBuilder builder, ServiceCandidate registration)
    {
        if (registration.ServiceType.IsGenericTypeDefinition)
        {
            return;
        }

        AddNonKeyedEnumerableMethod
            .MakeGenericMethod(registration.ServiceType)
            .Invoke(null, [builder]);
    }

    /// <summary>执行 AddNonKeyedOpenGenericEnumerableSource 操作</summary>
    /// <param name="builder">builder 参数</param>
    /// <param name="registrations">registrations 参数</param>
    private static void AddNonKeyedOpenGenericEnumerableSource(
        ContainerBuilder builder,
        IReadOnlyList<ServiceCandidate> registrations)
    {
        var serviceDefinitions = registrations
            .Where(registration => registration.Key is null)
            .Select(registration => registration.ServiceType)
            .Where(serviceType => serviceType.IsGenericTypeDefinition)
            .Distinct()
            .ToList();

        if (serviceDefinitions.Count == 0)
        {
            return;
        }

        builder.RegisterSource(new NonKeyedOpenGenericEnumerableRegistrationSource(serviceDefinitions));
    }

    /// <summary>执行 AddNonKeyedEnumerableCore 操作</summary>
    /// <typeparam name="TService">TService 类型参数</typeparam>
    /// <param name="builder">builder 参数</param>
    private static void AddNonKeyedEnumerableCore<TService>(ContainerBuilder builder)
        where TService : notnull
    {
        builder.Register(context => new[] { context.Resolve<TService>() })
            .As<IEnumerable<TService>>();
    }

    /// <summary>执行 ApplyTypedInterception 操作</summary>
    /// <typeparam name="TLimit">TLimit 类型参数</typeparam>
    /// <typeparam name="TRegistrationStyle">TRegistrationStyle 类型参数</typeparam>
    /// <param name="registrationBuilder">registrationBuilder 参数</param>
    /// <param name="registration">registration 参数</param>
    /// <param name="report">report 参数</param>
    private static void ApplyTypedInterception<TLimit, TRegistrationStyle>(
        IRegistrationBuilder<TLimit, ConcreteReflectionActivatorData, TRegistrationStyle> registrationBuilder,
        ServiceCandidate registration,
        InterceptionReport report)
    {
        if (ApplyInterfaceInterception(registrationBuilder, registration, report))
        {
            return;
        }

        if (HasEnabledCarrier(report, registration, CastleClassProxy))
        {
            registrationBuilder
                .EnableClassInterceptors()
                .InterceptedBy(typeof(CastleAsyncInterceptorAdapter));
        }
    }

    /// <summary>执行 ApplyInterfaceInterception 操作</summary>
    /// <typeparam name="TLimit">TLimit 类型参数</typeparam>
    /// <typeparam name="TActivatorData">TActivatorData 类型参数</typeparam>
    /// <typeparam name="TRegistrationStyle">TRegistrationStyle 类型参数</typeparam>
    /// <param name="registrationBuilder">registrationBuilder 参数</param>
    /// <param name="registration">registration 参数</param>
    /// <param name="report">report 参数</param>
    /// <returns>ApplyInterfaceInterception 的执行结果</returns>
    private static bool ApplyInterfaceInterception<TLimit, TActivatorData, TRegistrationStyle>(
        IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> registrationBuilder,
        ServiceCandidate registration,
        InterceptionReport report)
    {
        if (HasEnabledCarrier(report, registration, CastleInterfaceProxy))
        {
            registrationBuilder
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof(CastleAsyncInterceptorAdapter));
            return true;
        }

        return false;
    }

    /// <summary>执行 HasEnabledCarrier 操作</summary>
    /// <param name="report">report 参数</param>
    /// <param name="registration">registration 参数</param>
    /// <param name="carrier">carrier 参数</param>
    /// <returns>HasEnabledCarrier 的执行结果</returns>
    private static bool HasEnabledCarrier(
        InterceptionReport report,
        ServiceCandidate registration,
        string carrier)
    {
        var serviceTypeName = TypeName(registration.ServiceType);
        var implementationTypeName = TypeName(registration.ImplementationType);

        return report.Items.Any(item =>
            item.Status == Enabled
            && item.Carrier == carrier
            && item.ServiceTypeName == serviceTypeName
            && item.ImplementationTypeName == implementationTypeName);
    }

    /// <summary>执行 ApplyLifetime 操作</summary>
    /// <typeparam name="TLimit">TLimit 类型参数</typeparam>
    /// <typeparam name="TActivatorData">TActivatorData 类型参数</typeparam>
    /// <typeparam name="TRegistrationStyle">TRegistrationStyle 类型参数</typeparam>
    /// <param name="registrationBuilder">registrationBuilder 参数</param>
    /// <param name="lifetime">lifetime 参数</param>
    private static void ApplyLifetime<TLimit, TActivatorData, TRegistrationStyle>(
        IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> registrationBuilder,
        DependencyLifetime lifetime)
    {
        _ = lifetime switch
        {
            DependencyLifetime.Transient => registrationBuilder.InstancePerDependency(),
            DependencyLifetime.Scoped => registrationBuilder.InstancePerLifetimeScope(),
            DependencyLifetime.Singleton => registrationBuilder.SingleInstance(),
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, "未知服务生命周期"),
        };
    }

    /// <summary>执行 TypeName 操作</summary>
    /// <param name="type">type 参数</param>
    /// <returns>TypeName 的执行结果</returns>
    private static string TypeName(Type type) => type.FullName ?? type.Name;

    /// <summary>表示 NonKeyedOpenGenericEnumerableRegistrationSource 类型</summary>
    private sealed class NonKeyedOpenGenericEnumerableRegistrationSource : IRegistrationSource
    {
        /// <summary>表示 _serviceDefinitions 字段</summary>
        private readonly HashSet<Type> _serviceDefinitions;

        /// <summary>初始化 NonKeyedOpenGenericEnumerableRegistrationSource 实例</summary>
        /// <param name="serviceDefinitions">serviceDefinitions 参数</param>
        public NonKeyedOpenGenericEnumerableRegistrationSource(IEnumerable<Type> serviceDefinitions)
        {
            _serviceDefinitions = serviceDefinitions.ToHashSet();
        }

        /// <summary>表示 IsAdapterForIndividualComponents 属性</summary>
        public bool IsAdapterForIndividualComponents => false;

        /// <summary>执行 RegistrationsFor 操作</summary>
        /// <param name="service">service 参数</param>
        /// <param name="registrationAccessor">registrationAccessor 参数</param>
        /// <returns>RegistrationsFor 的执行结果</returns>
        public IEnumerable<IComponentRegistration> RegistrationsFor(
            Service service,
            Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
        {
            if (service is not TypedService typedService)
            {
                return [];
            }

            var collectionType = typedService.ServiceType;
            if (!collectionType.IsGenericType
                || collectionType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
            {
                return [];
            }

            var serviceType = collectionType.GetGenericArguments()[0];
            if (!serviceType.IsGenericType
                || serviceType.IsGenericTypeDefinition
                || serviceType.ContainsGenericParameters
                || !_serviceDefinitions.Contains(serviceType.GetGenericTypeDefinition()))
            {
                return [];
            }

            var registration = RegistrationBuilder
                .ForDelegate(collectionType, (context, _) =>
                {
                    var resolvedService = context.Resolve(serviceType);
                    var services = Array.CreateInstance(serviceType, 1);
                    services.SetValue(resolvedService, 0);
                    return services;
                })
                .As(collectionType)
                .CreateRegistration();

            return [registration];
        }
    }
}
