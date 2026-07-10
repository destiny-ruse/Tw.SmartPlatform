using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tw.Threading;
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
    /// <remarks>
    /// 如果业务应用提供了自己的 <see cref="IEntityTranslationStore"/> 实现，应将其注册为 Singleton。
    /// 若注册为 Scoped，会产生俘虏依赖问题：单例 <see cref="IEntityTranslationService"/> 将在整个
    /// 应用生命周期内持有同一个 Scoped 实例，导致作用域语义失效。
    /// </remarks>
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

    /// <summary>
    /// 说明LoadJSONResources在当前类型中的职责
    /// </summary>
    /// <param name="options">用于配置当前组件行为的选项</param>
    /// <returns>匹配当前查询条件的结果集合</returns>
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

            var fileName = Path.GetFileName(path);
            var dotIndex = fileName.IndexOf('.', StringComparison.Ordinal);
            var resourceName = dotIndex < 0 ? fileName : fileName[..dotIndex];
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new TwConfigurationException($"无法从路径推断资源名称（文件名必须以非点字符开头）：{path}");
            }

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new TwConfigurationException($"无法读取 JSON 多语言资源文件：{path}", ex);
            }

            resources.Add(JsonTextResourceParser.Parse(resourceName, path, json));
        }

        return resources;
    }
}
