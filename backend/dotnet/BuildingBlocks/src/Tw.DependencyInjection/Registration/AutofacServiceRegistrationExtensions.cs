using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tw.DependencyInjection.Configuration;
using Tw.DependencyInjection.Discovery;
using Tw.DependencyInjection.DynamicProxy;
using Tw.DependencyInjection.Registration;

namespace Tw.DependencyInjection;

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
        var interceptionReport = InterceptionRegistrationPlanner.Plan(
            plan.Registrations,
            new AttributeInterceptorSelector());

        AutofacServiceRegistrationExecutor.Apply(builder, plan, interceptionReport);

        return builder;
    }

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
