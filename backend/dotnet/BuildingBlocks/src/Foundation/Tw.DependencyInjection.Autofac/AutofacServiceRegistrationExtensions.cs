using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tw.DependencyInjection.Configuration;
using Tw.DependencyInjection.Discovery;
using Tw.Castle.Core;
using Tw.DependencyInjection.Autofac.Registration;
using Tw.DependencyInjection.Registration;

namespace Tw.DependencyInjection.Autofac;

/// <summary>
/// 自动发现并注册服务的 Autofac <see cref="ContainerBuilder"/> 扩展
/// </summary>
public static class AutofacServiceRegistrationExtensions
{
    /// <summary>
    /// 根据配置节 <c>Tw:DependencyInjection</c> 发现程序集并通过 Autofac 原生 API 注册服务
    /// </summary>
    /// <param name="builder">Autofac 容器构建器</param>
    /// <param name="configuration">应用配置根</param>
    /// <returns>同一容器构建器，便于链式调用</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> 或 <paramref name="configuration"/> 为 null 时抛出</exception>
    public static ContainerBuilder AddServiceRegistration(
        this ContainerBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        return AddServiceRegistration(builder, configuration, new RuntimeAssemblySource());
    }

    /// <summary>
    /// 使用指定程序集来源发现并注册服务，便于测试注入受控扫描范围
    /// </summary>
    /// <param name="builder">Autofac 容器构建器</param>
    /// <param name="configuration">应用配置根</param>
    /// <param name="assemblySource">程序集候选来源</param>
    /// <returns>同一容器构建器，便于链式调用</returns>
    /// <exception cref="ArgumentNullException">任一参数为 null 时抛出</exception>
    internal static ContainerBuilder AddServiceRegistration(
        this ContainerBuilder builder,
        IConfiguration configuration,
        IAssemblySource assemblySource)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(assemblySource);

        var options = new ServiceRegistrationOptions();
        configuration.GetSection("Tw:DependencyInjection").Bind(options);

        var discovery = AssemblyDiscoverer.Discover(options, assemblySource);
        var topologyLevels = discovery.Report.Topology.ToDictionary(
            entry => entry.AssemblyName,
            entry => entry.Level,
            StringComparer.Ordinal);

        var typesByAssemblyName = discovery.OrderedAssemblies.ToDictionary(
            assembly => assembly.GetName().Name!,
            SafeGetTypes,
            StringComparer.Ordinal);

        var optionsPlan = OptionsBindingPlanner.Plan(
            discovery.OrderedAssemblies,
            typesByAssemblyName,
            configuration);

        var optionServices = new ServiceCollection();
        OptionsBindingExecutor.Apply(optionServices, configuration, optionsPlan.Candidates);
        optionServices.TryAddSingleton(optionsPlan.Report);
        builder.Populate(optionServices);

        var plan = ServiceRegistrationPlanner.Plan(
            discovery.OrderedAssemblies,
            typesByAssemblyName,
            topologyLevels,
            discovery.ReachabilityGraph,
            options);

        ConstructorKeyedServiceValidator.Validate(plan.Registrations);
        var interceptionCandidates = plan.Registrations
            .Select(registration => new InterceptionCandidate(
                registration.ServiceType,
                registration.ImplementationType))
            .ToList();
        var interceptionPlan = InterceptionRegistrationPlanner.Plan(
            interceptionCandidates,
            new AttributeInterceptorSelector());

        AutofacServiceRegistrationExecutor.Apply(builder, plan, interceptionPlan.Report);
        RegisterInterceptorRegistrationValidation(builder, interceptionPlan.RequiredInterceptorTypes);

        return builder;
    }

    /// <summary>
    /// 注册容器构建回调，在启动期校验所有被命中的拦截器类型已在容器中注册。
    /// 拦截器可经自动注册（实现生命周期标记接口）或由组合根显式注册，
    /// 校验必须放在容器构建阶段才能同时覆盖两种来源。
    /// </summary>
    private static void RegisterInterceptorRegistrationValidation(
        ContainerBuilder builder,
        IReadOnlyCollection<Type> requiredInterceptorTypes)
    {
        if (requiredInterceptorTypes.Count == 0)
        {
            return;
        }

        builder.RegisterBuildCallback(scope =>
        {
            foreach (var interceptorType in requiredInterceptorTypes)
            {
                if (!scope.IsRegistered(interceptorType))
                {
                    throw new ServiceRegistrationException(
                        $"拦截器类型未注册: {interceptorType.FullName ?? interceptorType.Name}。" +
                        "拦截器必须以自身类型注册，例如实现 ITransientDependency 等生命周期标记接口参与自动注册，" +
                        "或在组合根显式注册。");
                }
            }
        });
    }

    /// <summary>
    /// 说明Safe读取类型集合在当前类型中的职责
    /// </summary>
    /// <param name="assembly">用于提供assembly</param>
    /// <returns>匹配当前查询条件的结果集合</returns>
    private static IReadOnlyList<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null).Select(type => type!).ToList();
        }
    }
}
