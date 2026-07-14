using Microsoft.Extensions.Localization;
using Tw.Localization;
using CoreLocalizationOptions = Tw.Localization.LocalizationOptions;

namespace Tw.AspNetCore.Localization;

/// <summary>
/// 创建只读取静态文本快照的 ASP.NET Core 字符串本地化器
/// </summary>
public sealed class StaticSnapshotStringLocalizerFactory : IStringLocalizerFactory
{
    /// <summary>
    /// 提供同步文本查找能力的静态快照
    /// </summary>
    private readonly IStaticTextSnapshot _snapshot;

    /// <summary>
    /// 提供当前请求本地化上下文的访问器
    /// </summary>
    private readonly ICurrentLocalizationContextAccessor _accessor;

    /// <summary>
    /// 提供默认文化与支持文化范围的核心配置
    /// </summary>
    private readonly CoreLocalizationOptions _options;

    /// <summary>
    /// 初始化静态快照字符串本地化器工厂
    /// </summary>
    /// <param name="snapshot">提供同步文本查找能力的静态快照</param>
    /// <param name="accessor">当前请求本地化上下文访问器</param>
    /// <param name="options">包含默认文化与支持文化范围的核心配置</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="snapshot"/>、<paramref name="accessor"/> 或 <paramref name="options"/> 为 <see langword="null"/> 时抛出
    /// </exception>
    public StaticSnapshotStringLocalizerFactory(
        IStaticTextSnapshot snapshot,
        ICurrentLocalizationContextAccessor accessor,
        CoreLocalizationOptions options)
    {
        _snapshot = Check.NotNull(snapshot);
        _accessor = Check.NotNull(accessor);
        _options = Check.NotNull(options);
    }

    /// <summary>
    /// 使用资源类型的简单名称创建静态快照字符串本地化器
    /// </summary>
    /// <param name="resourceSource">标识本地化资源范围的类型</param>
    /// <returns>绑定到资源类型简单名称的本地化器</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resourceSource"/> 为 <see langword="null"/> 时抛出</exception>
    /// <remarks>
    /// 不同命名空间中的同名类型会映射到同一资源名称，调用方必须保证快照资源名称唯一
    /// </remarks>
    public IStringLocalizer Create(Type resourceSource)
    {
        Check.NotNull(resourceSource);
        return new StaticSnapshotStringLocalizer(
            _snapshot,
            _accessor,
            _options,
            resourceSource.Name);
    }

    /// <summary>
    /// 使用资源基础名称创建静态快照字符串本地化器
    /// </summary>
    /// <param name="baseName">直接用于静态快照查找的资源基础名称</param>
    /// <param name="location">静态快照适配器忽略的程序集位置标识</param>
    /// <returns>绑定到资源基础名称的本地化器</returns>
    /// <exception cref="ArgumentException"><paramref name="baseName"/> 为空白字符串时抛出</exception>
    public IStringLocalizer Create(string baseName, string location)
    {
        Check.NotNullOrWhiteSpace(baseName);
        return new StaticSnapshotStringLocalizer(_snapshot, _accessor, _options, baseName);
    }
}
