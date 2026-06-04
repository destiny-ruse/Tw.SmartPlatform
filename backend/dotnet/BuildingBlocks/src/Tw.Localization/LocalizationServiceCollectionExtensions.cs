using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tw.Context;
using Tw.Exceptions;
using Tw.Localization.Json;

namespace Tw.Localization;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供本地化核心能力注册扩展
/// </summary>
public static class LocalizationServiceCollectionExtensions
{
    /// <summary>
    /// 注册本地化核心能力，包括 <see cref="ITextLocalizer"/>、<see cref="IEntityTranslationService"/>
    /// 和 <see cref="IStaticTextSnapshot"/>，并加载 <see cref="LocalizationOptions.JsonResourcePaths"/>
    /// 中声明的静态 JSON 资源
    /// </summary>
    /// <param name="services">服务容器</param>
    /// <param name="configure">用于配置 <see cref="LocalizationOptions"/> 的委托</param>
    /// <returns>同一 <see cref="IServiceCollection"/> 实例，便于链式调用</returns>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="services"/> 或 <paramref name="configure"/> 为 <see langword="null"/> 时抛出
    /// </exception>
    /// <exception cref="TwConfigurationException">
    /// 当 <see cref="LocalizationOptions"/> 校验不通过，或声明的 JSON 资源文件路径不存在，
    /// 或 JSON 文件格式不合规时抛出
    /// </exception>
    public static IServiceCollection AddLocalization(
        this IServiceCollection services,
        Action<LocalizationOptions> configure)
    {
        Check.NotNull(services);
        Check.NotNull(configure);

        var options = new LocalizationOptions();
        configure(options);
        options.Validate();

        var resources = LoadJsonResources(options);

        services.AddCancellationTokenProvider();
        services.AddSingleton(options);
        services.AddSingleton<ITextResourceContributor>(new JsonTextResourceContributor(resources, priority: 0));
        services.AddSingleton<IStaticTextSnapshot>(new StaticTextSnapshot(resources));
        services.AddSingleton<ITextLocalizer, TextLocalizer>();
        services.AddSingleton<IEntityTranslationService, EntityTranslationService>();
        services.TryAddSingleton<IEntityTranslationStore, EmptyEntityTranslationStore>();

        return services;
    }

    private static IReadOnlyList<JsonTextResource> LoadJsonResources(LocalizationOptions options)
    {
        if (options.JsonResourcePaths.Count == 0)
        {
            return [];
        }

        var resources = new List<JsonTextResource>(options.JsonResourcePaths.Count);

        foreach (var path in options.JsonResourcePaths)
        {
            if (!File.Exists(path))
            {
                throw new TwConfigurationException($"JSON 多语言资源路径不存在：{path}");
            }

            var resourceName = Check.NotNullOrWhiteSpace(Path.GetFileName(path).Split('.')[0]);
            var json = File.ReadAllText(path);
            resources.Add(JsonTextResourceParser.Parse(resourceName, path, json));
        }

        return resources;
    }
}
