using System.Globalization;
using Microsoft.Extensions.Localization;
using Tw.Localization;
using CoreLocalizationOptions = Tw.Localization.LocalizationOptions;

namespace Tw.AspNetCore.Localization;

/// <summary>
/// 将静态文本快照适配为 ASP.NET Core 同步字符串本地化接口
/// </summary>
/// <remarks>
/// 本地化器只查询 <see cref="IStaticTextSnapshot"/>，不会访问动态覆盖来源
/// 文化回退顺序由 <see cref="CultureFallback.Expand"/> 统一计算
/// </remarks>
public sealed class StaticSnapshotStringLocalizer : IStringLocalizer
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
    /// 当前本地化器查找的资源名称
    /// </summary>
    private readonly string _resourceName;

    /// <summary>
    /// 初始化指定资源的静态快照字符串本地化器
    /// </summary>
    /// <param name="snapshot">提供同步文本查找能力的静态快照</param>
    /// <param name="accessor">当前请求本地化上下文访问器</param>
    /// <param name="options">包含默认文化与支持文化范围的核心配置</param>
    /// <param name="resourceName">在快照中定位文本条目的资源名称</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="snapshot"/>、<paramref name="accessor"/> 或 <paramref name="options"/> 为 <see langword="null"/> 时抛出
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="resourceName"/> 为空白字符串时抛出</exception>
    public StaticSnapshotStringLocalizer(
        IStaticTextSnapshot snapshot,
        ICurrentLocalizationContextAccessor accessor,
        CoreLocalizationOptions options,
        string resourceName)
    {
        _snapshot = Check.NotNull(snapshot);
        _accessor = Check.NotNull(accessor);
        _options = Check.NotNull(options);
        _resourceName = Check.NotNullOrWhiteSpace(resourceName);
    }

    /// <summary>
    /// 按文本键查询当前文化回退链中的静态快照文本
    /// </summary>
    /// <param name="name">需要查询的文本键</param>
    /// <returns>命中时包含翻译值，未命中时包含原始文本键</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> 为 <see langword="null"/> 时抛出</exception>
    public LocalizedString this[string name]
    {
        get
        {
            Check.NotNull(name);
            var hit = _snapshot.Find(_resourceName, name, BuildCandidates());

            return hit is not null
                ? new LocalizedString(name, hit.Value, resourceNotFound: false)
                : new LocalizedString(name, name, resourceNotFound: true);
        }
    }

    /// <summary>
    /// 按文本键查询静态快照文本并使用当前文化格式化占位符
    /// </summary>
    /// <param name="name">需要查询的文本键</param>
    /// <param name="arguments">传递给字符串格式化器的占位符参数</param>
    /// <returns>命中时包含格式化后的翻译值，未命中时包含未格式化的原始文本键</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="FormatException">命中的翻译模板格式无效时抛出</exception>
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var localizedString = this[name];
            if (localizedString.ResourceNotFound)
            {
                return localizedString;
            }

            var formatted = string.Format(CultureInfo.CurrentCulture, localizedString.Value, arguments);
            return new LocalizedString(name, formatted, resourceNotFound: false);
        }
    }

    /// <summary>
    /// 枚举当前资源在指定文化范围内的全部静态快照文本
    /// </summary>
    /// <param name="includeParentCultures">是否包含父级文化与默认文化的回退结果</param>
    /// <returns>按静态快照合并规则得到的本地化字符串序列</returns>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var context = _accessor.Current ?? new LocalizationContext(_options.DefaultCulture);
        IReadOnlyList<string> candidates = includeParentCultures
            ? CultureFallback.Expand(context, _options)
            : [context.CultureName];

        return _snapshot
            .GetAll(_resourceName, candidates)
            .Select(entry => new LocalizedString(
                entry.Key,
                entry.Value.Value,
                resourceNotFound: false));
    }

    /// <summary>
    /// 构造当前请求的完整文化候选顺序
    /// </summary>
    /// <returns>从当前文化展开到默认文化的候选列表</returns>
    private IReadOnlyList<string> BuildCandidates()
    {
        var context = _accessor.Current ?? new LocalizationContext(_options.DefaultCulture);
        return CultureFallback.Expand(context, _options);
    }
}
