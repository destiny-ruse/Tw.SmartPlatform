using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tw.DependencyInjection.Configuration;
using Tw.DependencyInjection.Discovery;
using Tw.DependencyInjection.Registration;

namespace Tw.DependencyInjection;

/// <summary>
/// 自动发现并注册服务的 <see cref="IServiceCollection"/> 扩展
/// </summary>
public static class ServiceCollectionRegistrationExtensions
{
    /// <summary>
    /// 根据配置节 <c>Tw:DependencyInjection</c> 发现程序集并注册服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">应用配置根</param>
    /// <returns>同一服务集合，便于链式调用</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> 或 <paramref name="configuration"/> 为 null 时抛出</exception>
    public static IServiceCollection AddServiceRegistration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return AddServiceRegistration(services, configuration, new RuntimeAssemblySource());
    }

    /// <summary>
    /// 使用指定程序集来源发现并注册服务，便于测试注入受控扫描范围
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">应用配置根</param>
    /// <param name="assemblySource">程序集候选来源</param>
    /// <returns>同一服务集合，便于链式调用</returns>
    /// <exception cref="ArgumentNullException">任一参数为 null 时抛出</exception>
    /// <remarks>
    /// <see cref="ServiceRegistrationReport"/> 以 <see cref="Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddSingleton{TService}(IServiceCollection, TService)"/>
    /// 注册，多次调用时保留首个报告，不会重复注册导致 <c>GetRequiredService</c> 歧义。
    /// <see cref="AddServiceRegistration(IServiceCollection, IConfiguration)"/> 设计为组合根处调用一次。
    /// </remarks>
    internal static IServiceCollection AddServiceRegistration(
        this IServiceCollection services,
        IConfiguration configuration,
        IAssemblySource assemblySource)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(assemblySource);

        var options = new ServiceRegistrationOptions();
        configuration.GetSection("Tw:DependencyInjection").Bind(options);

        var discovery = AssemblyDiscoverer.Discover(options, assemblySource);

        var topologyLevels = discovery.Report.Topology.ToDictionary(
            entry => entry.AssemblyName,
            entry => entry.Level,
            StringComparer.Ordinal);

        // OrderedAssemblies 来自 AssemblyDiscoverer，已按非 null 程序集名过滤，故此处 Name 必非 null
        var typesByAssemblyName = discovery.OrderedAssemblies.ToDictionary(
            assembly => assembly.GetName().Name!,
            SafeGetTypes,
            StringComparer.Ordinal);

        var optionsPlan = OptionsBindingPlanner.Plan(
            discovery.OrderedAssemblies,
            typesByAssemblyName,
            configuration);
        OptionsBindingExecutor.Apply(services, configuration, optionsPlan.Candidates);
        services.TryAddSingleton(optionsPlan.Report);

        var plan = ServiceRegistrationPlanner.Plan(
            discovery.OrderedAssemblies,
            typesByAssemblyName,
            topologyLevels,
            discovery.ReachabilityGraph,
            options);

        ConstructorKeyedServiceValidator.Validate(plan.Registrations);
        ServiceRegistrationExecutor.Apply(services, plan);
        services.TryAddSingleton(plan.Report);
        return services;
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
