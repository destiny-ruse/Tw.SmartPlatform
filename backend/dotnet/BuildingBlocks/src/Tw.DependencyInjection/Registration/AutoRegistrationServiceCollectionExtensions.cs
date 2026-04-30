using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tw.Core;
using Tw.DependencyInjection.Cancellation;
using Tw.DependencyInjection.Interception;
using Tw.DependencyInjection.Options;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 提供自动注册的 IServiceCollection 入口
/// </summary>
public static class AutoRegistrationServiceCollectionExtensions
{
    /// <summary>
    /// 配置自动注册所需的服务、扫描程序集和全局拦截器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">应用配置根</param>
    /// <param name="configure">自动注册配置回调</param>
    public static IServiceCollection AddAutoRegistration(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AutoRegistrationOptions>? configure = null)
    {
        Check.NotNull(services);
        Check.NotNull(configuration);

        var options = new AutoRegistrationOptions();
        configure?.Invoke(options);

        RegisterCancellationDefaults(services);
        RegisterGlobalInterceptors(services, options);
        RegisterMatcherTypes(services, options);
        RegisterEntryChainCache(services);

        if (options.OptionsAutoRegistrationEnabled && options.Assemblies.Count > 0)
        {
            services.AddOptionsAutoRegistration(configuration, options.Assemblies.ToArray());
        }

        services.RemoveAll<AutoRegistrationState>();
        services.RemoveAll<AutoRegistrationOptions>();
        services.AddSingleton(options);
        services.AddSingleton(new AutoRegistrationState(configuration, options));
        return services;
    }

    private static void RegisterCancellationDefaults(IServiceCollection services)
    {
        services.TryAddSingleton<CurrentCancellationTokenAccessor>();
        services.TryAddSingleton<ICurrentCancellationTokenAccessor>(
            provider => provider.GetRequiredService<CurrentCancellationTokenAccessor>());
        services.TryAddSingleton<ICancellationTokenProvider>(
            provider => provider.GetRequiredService<CurrentCancellationTokenAccessor>());
    }

    private static void RegisterGlobalInterceptors(
        IServiceCollection services,
        AutoRegistrationOptions options)
    {
        foreach (var interceptor in options.GlobalInterceptors)
        {
            services.TryAdd(ServiceDescriptor.Transient(interceptor.InterceptorType, interceptor.InterceptorType));
        }
    }

    private static void RegisterMatcherTypes(
        IServiceCollection services,
        AutoRegistrationOptions options)
    {
        foreach (var matcherType in options.Assemblies.SelectMany(GetLoadableTypes).Where(IsPublicConcreteMatcher))
        {
            options.AddMatcherType(matcherType);
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IInterceptorMatcher), matcherType));
        }
    }

    private static void RegisterEntryChainCache(IServiceCollection services)
    {
        services.TryAddSingleton(provider =>
        {
            var options = provider.GetRequiredService<AutoRegistrationOptions>();
            return new EntryChainCache(
                options.GlobalInterceptors,
                provider.GetServices<IInterceptorMatcher>());
        });
    }

    private static bool IsPublicConcreteMatcher(Type type)
        => typeof(IInterceptorMatcher).IsAssignableFrom(type)
           && !type.IsAbstract
           && !type.IsInterface
           && (type.IsPublic || type.IsNestedPublic)
           && type.GetConstructor(Type.EmptyTypes) is not null;

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }
}
