using Microsoft.Extensions.Localization;
using Tw.Localization;

namespace Tw.Localization.AspNetCore;

/// <summary>
/// <see cref="IStringLocalizerFactory"/> 的实现，基于 <see cref="IStaticTextSnapshot"/>
/// 为指定资源类型或资源名称创建 <see cref="TwStringLocalizer"/> 实例。
/// </summary>
/// <remarks>
/// 本工厂仅查询静态 JSON 快照，不访问任何动态文本覆盖来源。
/// 工厂持有注入的 <see cref="ICurrentLocalizationContextAccessor"/>，
/// 并在每次查找时读取其 <c>Current</c> 属性；
/// 工厂和访问器的 DI 生命周期由注册扩展方法决定，本类不预设具体组合。
/// </remarks>
public sealed class TwStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly IStaticTextSnapshot _snapshot;
    private readonly ICurrentLocalizationContextAccessor _accessor;
    private readonly LocalizationOptions _options;

    /// <summary>
    /// 初始化 <see cref="TwStringLocalizerFactory"/> 类的新实例
    /// </summary>
    /// <param name="snapshot">静态文本快照，提供同步文本查找能力</param>
    /// <param name="accessor">当前请求本地化上下文访问器</param>
    /// <param name="options">系统级本地化配置，提供默认文化与支持文化列表</param>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="snapshot"/>、<paramref name="accessor"/> 或 <paramref name="options"/> 为 <see langword="null"/> 时抛出
    /// </exception>
    public TwStringLocalizerFactory(
        IStaticTextSnapshot snapshot,
        ICurrentLocalizationContextAccessor accessor,
        LocalizationOptions options)
    {
        _snapshot = Check.NotNull(snapshot);
        _accessor = Check.NotNull(accessor);
        _options = Check.NotNull(options);
    }

    /// <summary>
    /// 为指定资源类型创建 <see cref="IStringLocalizer"/>，资源名称取自类型的简单名称（<see cref="Type.Name"/>）
    /// </summary>
    /// <param name="resourceSource">用于标识本地化资源范围的类型</param>
    /// <returns>以 <paramref name="resourceSource"/> 简单名称为资源名的 <see cref="TwStringLocalizer"/> 实例</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="resourceSource"/> 为 <see langword="null"/> 时抛出</exception>
    /// <remarks>
    /// 资源名称取自 <see cref="Type.Name"/>（不含命名空间）。
    /// 若不同命名空间下存在同名类型，两者将映射到同一资源名，导致查找结果混用。
    /// 调用方必须确保在同一快照中资源名称全局唯一。
    /// </remarks>
    public IStringLocalizer Create(Type resourceSource)
    {
        Check.NotNull(resourceSource);
        return new TwStringLocalizer(_snapshot, _accessor, _options, resourceSource.Name);
    }

    /// <summary>
    /// 为指定基础名称创建 <see cref="IStringLocalizer"/>。
    /// <paramref name="location"/> 参数在本适配器中被忽略——静态快照不依赖程序集位置信息。
    /// </summary>
    /// <param name="baseName">资源基础名称，直接用作 <see cref="TwStringLocalizer"/> 的资源名</param>
    /// <param name="location">程序集或位置标识符；本适配器忽略此参数</param>
    /// <returns>以 <paramref name="baseName"/> 为资源名的 <see cref="TwStringLocalizer"/> 实例</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="baseName"/> 为 <see langword="null"/> 或空白字符串时抛出</exception>
    public IStringLocalizer Create(string baseName, string location)
    {
        Check.NotNullOrWhiteSpace(baseName);
        // location 在静态快照适配器中无意义，直接忽略
        return new TwStringLocalizer(_snapshot, _accessor, _options, baseName);
    }
}
