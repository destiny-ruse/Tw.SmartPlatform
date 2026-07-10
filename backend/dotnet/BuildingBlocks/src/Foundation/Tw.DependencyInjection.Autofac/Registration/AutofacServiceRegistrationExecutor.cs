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
    /// <summary>
    /// 保存当前类型处理流程依赖的添加NonKeyedEnumerableMethod
    /// </summary>
    private static readonly MethodInfo AddNonKeyedEnumerableMethod = typeof(AutofacServiceRegistrationExecutor)
        .GetMethod(nameof(AddNonKeyedEnumerableCore), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// 当前类型内部复用的Enabled常量值
    /// </summary>
    private const string Enabled = "enabled";
    /// <summary>
    /// 当前类型内部复用的CastleInterface代理常量值
    /// </summary>
    private const string CastleInterfaceProxy = "CastleInterfaceProxy";
    /// <summary>
    /// 当前类型内部复用的CastleClass代理常量值
    /// </summary>
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

    /// <summary>
    /// 说明RegisterDynamicProxy服务集合在当前类型中的职责
    /// </summary>
    /// <param name="builder">承载服务注册或主机配置的构建器</param>
    /// <param name="report">用于提供报告</param>
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

    /// <summary>
    /// 注册服务所需服务
    /// </summary>
    /// <param name="builder">承载服务注册或主机配置的构建器</param>
    /// <param name="registration">用于提供registration</param>
    /// <param name="report">用于提供报告</param>
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

    /// <summary>
    /// 注册NonKeyed所需服务
    /// </summary>
    /// <param name="builder">承载服务注册或主机配置的构建器</param>
    /// <param name="registration">用于提供registration</param>
    /// <param name="report">用于提供报告</param>
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

    /// <summary>
    /// 注册Keyed所需服务
    /// </summary>
    /// <param name="builder">承载服务注册或主机配置的构建器</param>
    /// <param name="registration">用于提供registration</param>
    /// <param name="report">用于提供报告</param>
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

    /// <summary>
    /// 注册KeyedEntry所需服务
    /// </summary>
    /// <param name="builder">承载服务注册或主机配置的构建器</param>
    /// <param name="registration">用于提供registration</param>
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

    /// <summary>
    /// 注册NonKeyedEnumerable所需服务
    /// </summary>
    /// <param name="builder">承载服务注册或主机配置的构建器</param>
    /// <param name="registration">用于提供registration</param>
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

    /// <summary>
    /// 注册NonKeyedOpenGenericEnumerableSource所需服务
    /// </summary>
    /// <param name="builder">承载服务注册或主机配置的构建器</param>
    /// <param name="registrations">用于提供registrations</param>
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

    /// <summary>
    /// 注册NonKeyedEnumerableCore所需服务
    /// </summary>
    /// <typeparam name="TService">响应数据的运行时类型</typeparam>
    /// <param name="builder">承载服务注册或主机配置的构建器</param>
    private static void AddNonKeyedEnumerableCore<TService>(ContainerBuilder builder)
        where TService : notnull
    {
        builder.Register(context => new[] { context.Resolve<TService>() })
            .As<IEnumerable<TService>>();
    }

    /// <summary>
    /// 说明ApplyTypedInterception在当前类型中的职责
    /// </summary>
    /// <typeparam name="TLimit">响应数据的运行时类型</typeparam>
    /// <typeparam name="TRegistrationStyle">响应数据的运行时类型</typeparam>
    /// <param name="registrationBuilder">用于提供registrationBuilder</param>
    /// <param name="registration">用于提供registration</param>
    /// <param name="report">用于提供报告</param>
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

    /// <summary>
    /// 说明ApplyInterfaceInterception在当前类型中的职责
    /// </summary>
    /// <typeparam name="TLimit">响应数据的运行时类型</typeparam>
    /// <typeparam name="TActivatorData">响应数据的运行时类型</typeparam>
    /// <typeparam name="TRegistrationStyle">响应数据的运行时类型</typeparam>
    /// <param name="registrationBuilder">用于提供registrationBuilder</param>
    /// <param name="registration">用于提供registration</param>
    /// <param name="report">用于提供报告</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
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

    /// <summary>
    /// 说明存在EnabledCarrier在当前类型中的职责
    /// </summary>
    /// <param name="report">用于提供报告</param>
    /// <param name="registration">用于提供registration</param>
    /// <param name="carrier">用于提供carrier</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
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

    /// <summary>
    /// 说明ApplyLifetime在当前类型中的职责
    /// </summary>
    /// <typeparam name="TLimit">响应数据的运行时类型</typeparam>
    /// <typeparam name="TActivatorData">响应数据的运行时类型</typeparam>
    /// <typeparam name="TRegistrationStyle">响应数据的运行时类型</typeparam>
    /// <param name="registrationBuilder">用于提供registrationBuilder</param>
    /// <param name="lifetime">用于提供lifetime</param>
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

    /// <summary>
    /// 说明类型Name在当前类型中的职责
    /// </summary>
    /// <param name="type">用于提供类型</param>
    /// <returns>方法计算得到的文本值</returns>
    private static string TypeName(Type type) => type.FullName ?? type.Name;

    /// <summary>
    /// 封装NonKeyedOpenGenericEnumerableRegistrationSource相关的数据和行为
    /// </summary>
    private sealed class NonKeyedOpenGenericEnumerableRegistrationSource : IRegistrationSource
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的服务Definitions
        /// </summary>
        private readonly HashSet<Type> _serviceDefinitions;

        /// <summary>
        /// 初始化 NonKeyedOpenGenericEnumerableRegistrationSource 实例
        /// </summary>
        /// <param name="serviceDefinitions">用于提供服务Definitions</param>
        public NonKeyedOpenGenericEnumerableRegistrationSource(IEnumerable<Type> serviceDefinitions)
        {
            _serviceDefinitions = serviceDefinitions.ToHashSet();
        }

        /// <summary>
        /// sAdapter针对IndividualComponents在当前对象中的业务含义
        /// </summary>
        public bool IsAdapterForIndividualComponents => false;

        /// <summary>
        /// 说明RegistrationsFor在当前类型中的职责
        /// </summary>
        /// <param name="service">用于提供服务</param>
        /// <param name="registrationAccessor">用于提供registrationAccessor</param>
        /// <returns>匹配当前查询条件的结果集合</returns>
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
