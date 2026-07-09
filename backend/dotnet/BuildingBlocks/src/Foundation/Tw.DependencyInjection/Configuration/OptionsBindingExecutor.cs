using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tw.DependencyInjection.Abstractions.Configuration;

namespace Tw.DependencyInjection.Configuration;

/// <summary>
/// 将 Options 绑定计划写入 <see cref="IServiceCollection"/>
/// </summary>
internal static class OptionsBindingExecutor
{
    private static readonly MethodInfo ApplyOneMethod = typeof(OptionsBindingExecutor)
        .GetMethod(nameof(ApplyOne), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// 应用 Options 绑定候选
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">应用配置根</param>
    /// <param name="candidates">Options 绑定候选</param>
    public static void Apply(
        IServiceCollection services,
        IConfiguration configuration,
        IReadOnlyList<OptionsBindingCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(candidates);

        foreach (var candidate in candidates)
        {
            try
            {
                ApplyOneMethod
                    .MakeGenericMethod(candidate.OptionsType)
                    .Invoke(null, [services, configuration, candidate]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }
    }

    private static void ApplyOne<TOptions>(
        IServiceCollection services,
        IConfiguration configuration,
        OptionsBindingCandidate candidate)
        where TOptions : class, IConfigurableOptions
    {
        var section = configuration.GetSection(candidate.SectionPath);
        if (!candidate.SectionExists)
        {
            throw new ServiceRegistrationException($"必填配置节缺失: {candidate.SectionPath}");
        }

        services.AddOptions<TOptions>(candidate.Name)
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (candidate.ValidatorType is not null)
        {
            services.AddSingleton(typeof(IValidateOptions<TOptions>), candidate.ValidatorType);
        }

        if (typeof(IConfigurableOptions<TOptions>).IsAssignableFrom(typeof(TOptions)))
        {
            var wrapperType = typeof(ConfigurableOptionsPostConfigure<>).MakeGenericType(typeof(TOptions));
            var wrapper = Activator.CreateInstance(wrapperType, candidate.Name, section)!;
            services.AddSingleton(typeof(IPostConfigureOptions<TOptions>), wrapper);
        }
    }
}
