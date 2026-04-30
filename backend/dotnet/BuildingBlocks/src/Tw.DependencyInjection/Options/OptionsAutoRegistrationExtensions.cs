using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Tw.DependencyInjection.Options;

/// <summary>
/// 提供从程序集自动注册配置选项的扩展方法。
/// </summary>
public static class OptionsAutoRegistrationExtensions
{
    /// <summary>
    /// 扫描指定程序集（按类库约定），自动注册配置选项类型。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">应用程序配置根。</param>
    /// <param name="assemblies">以类库约定扫描的程序集；类型必须实现 <c>IConfigurableOptions</c> 或标注 <c>[ConfigurationSection]</c>。</param>
    /// <returns>同一 <see cref="IServiceCollection"/> 实例，支持链式调用。</returns>
    public static IServiceCollection AddOptionsAutoRegistration(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies)
    {
        var descriptors = OptionsRegistrationScanner.Scan(assemblies);
        return RegisterDescriptors(services, configuration, descriptors);
    }

    /// <summary>
    /// 扫描一个入口程序集（允许名称后缀约定）以及若干类库程序集，自动注册配置选项类型。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">应用程序配置根。</param>
    /// <param name="entryAssembly">入口程序集，额外允许类型名后缀为 <c>Options</c> 或 <c>Settings</c> 的类型。</param>
    /// <param name="additionalAssemblies">以类库约定扫描的附加程序集。</param>
    /// <returns>同一 <see cref="IServiceCollection"/> 实例，支持链式调用。</returns>
    public static IServiceCollection AddOptionsAutoRegistration(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly entryAssembly,
        params Assembly[] additionalAssemblies)
    {
        var descriptors = OptionsRegistrationScanner.Scan(additionalAssemblies, entryAssembly);
        return RegisterDescriptors(services, configuration, descriptors);
    }

    // ---- internal registration logic ----

    private static IServiceCollection RegisterDescriptors(
        IServiceCollection services,
        IConfiguration configuration,
        IReadOnlyList<OptionsRegistrationDescriptor> descriptors)
    {
        foreach (var d in descriptors)
        {
            RegisterOne(services, configuration, d);
        }

        return services;
    }

    private static void RegisterOne(
        IServiceCollection services,
        IConfiguration configuration,
        OptionsRegistrationDescriptor d)
    {
        var section = configuration.GetSection(d.SectionName);
        var optionsName = d.OptionsName ?? Microsoft.Extensions.Options.Options.DefaultName;

        // services.AddOptions<TOptions>(optionsName)
        var addOptionsMethod = typeof(OptionsServiceCollectionExtensions)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Single(m =>
                m.Name == nameof(OptionsServiceCollectionExtensions.AddOptions)
                && m.IsGenericMethod
                && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType == typeof(string));

        var builder = addOptionsMethod
            .MakeGenericMethod(d.OptionsType)
            .Invoke(null, [services, optionsName])!;

        var builderType = typeof(OptionsBuilder<>).MakeGenericType(d.OptionsType);

        // .Bind(section)
        var bindMethod = typeof(OptionsBuilderConfigurationExtensions)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .First(m =>
                m.Name == nameof(OptionsBuilderConfigurationExtensions.Bind)
                && m.IsGenericMethod
                && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType == typeof(IConfiguration));
        bindMethod.MakeGenericMethod(d.OptionsType).Invoke(null, [builder, section]);

        // .ValidateDataAnnotations()
        var validateDAMethod = typeof(OptionsBuilderDataAnnotationsExtensions)
            .GetMethod(nameof(OptionsBuilderDataAnnotationsExtensions.ValidateDataAnnotations))!
            .MakeGenericMethod(d.OptionsType);
        validateDAMethod.Invoke(null, [builder]);

        // .ValidateOnStart() — default true (fail-fast); skip only when explicitly set to false
        var shouldValidateOnStart = d.ValidateOnStart ?? true;
        if (shouldValidateOnStart)
        {
            var validateOnStartMethod = typeof(OptionsBuilderExtensions)
                .GetMethod("ValidateOnStart")!
                .MakeGenericMethod(d.OptionsType);
            validateOnStartMethod.Invoke(null, [builder]);
        }

        // DirectInject — only when OptionsName is null (default instance)
        if (d.DirectInject && d.OptionsName is null)
        {
            services.AddTransient(d.OptionsType, sp =>
            {
                var monitorType = typeof(IOptionsMonitor<>).MakeGenericType(d.OptionsType);
                var monitor = sp.GetRequiredService(monitorType);
                return monitor.GetType().GetProperty("CurrentValue")!.GetValue(monitor)!;
            });
        }
    }
}
