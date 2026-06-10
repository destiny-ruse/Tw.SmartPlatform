using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.DynamicProxy;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Diagnostics;
using Tw.DependencyInjection.DynamicProxy;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 将 <see cref="ServiceRegistrationPlan"/> 中的候选项执行写入 Autofac <see cref="ContainerBuilder"/>
/// </summary>
internal static class AutofacServiceRegistrationExecutor
{
    private static readonly MethodInfo AddNonKeyedEnumerableMethod = typeof(AutofacServiceRegistrationExecutor)
        .GetMethod(nameof(AddNonKeyedEnumerableCore), BindingFlags.NonPublic | BindingFlags.Static)!;

    private const string Enabled = "enabled";
    private const string CastleInterfaceProxy = "CastleInterfaceProxy";
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
    }

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

    private static void AddNonKeyedEnumerableCore<TService>(ContainerBuilder builder)
        where TService : notnull
    {
        builder.Register(context => new[] { context.Resolve<TService>() })
            .As<IEnumerable<TService>>();
    }

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

    private static string TypeName(Type type) => type.FullName ?? type.Name;
}
