using System.Globalization;
using Microsoft.Extensions.Localization;
using Tw.Localization;
using TwLocalizationOptions = Tw.Localization.LocalizationOptions;

namespace Tw.AspNetCore.Localization;

/// <summary>
/// <see cref="IStringLocalizer"/> 的同步静态快照适配器，将 <see cref="IStaticTextSnapshot"/>
/// 查找结果适配为 ASP.NET Core 标准本地化接口。
/// </summary>
/// <remarks>
/// 本类仅查询静态 JSON 快照（<see cref="IStaticTextSnapshot"/>），不会访问任何动态覆盖来源。
/// 文化回退链由 <see cref="CultureFallback.Expand"/> 统一展开。
/// 适合用于性能敏感路径，或无法使用 <c>async/await</c> 的场景。
/// </remarks>
public sealed class TwStringLocalizer : IStringLocalizer
{
    /// <summary>表示 _snapshot 字段</summary>
    private readonly IStaticTextSnapshot _snapshot;
    /// <summary>表示 _accessor 字段</summary>
    private readonly ICurrentLocalizationContextAccessor _accessor;
    /// <summary>表示 _options 字段</summary>
    private readonly TwLocalizationOptions _options;
    /// <summary>表示 _resourceName 字段</summary>
    private readonly string _resourceName;

    /// <summary>
    /// 初始化 <see cref="TwStringLocalizer"/> 类的新实例
    /// </summary>
    /// <param name="snapshot">静态文本快照，提供同步文本查找能力</param>
    /// <param name="accessor">当前请求本地化上下文访问器</param>
    /// <param name="options">系统级本地化配置，提供默认文化与支持文化列表</param>
    /// <param name="resourceName">资源名称，用于在快照中定位文本条目</param>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="snapshot"/>、<paramref name="accessor"/> 或 <paramref name="options"/> 为 <see langword="null"/> 时抛出
    /// </exception>
    /// <exception cref="ArgumentException">
    /// 当 <paramref name="resourceName"/> 为 <see langword="null"/> 或空白字符串时抛出
    /// </exception>
    public TwStringLocalizer(
        IStaticTextSnapshot snapshot,
        ICurrentLocalizationContextAccessor accessor,
        TwLocalizationOptions options,
        string resourceName)
    {
        _snapshot = Check.NotNull(snapshot);
        _accessor = Check.NotNull(accessor);
        _options = Check.NotNull(options);
        _resourceName = Check.NotNullOrWhiteSpace(resourceName);
    }

    /// <summary>
    /// 指定名称对应的本地化字符串
    /// </summary>
    /// <param name="name">文本键名</param>
    /// <returns>
    /// 当静态快照命中时，返回对应翻译值且 <see cref="LocalizedString.ResourceNotFound"/> 为 <see langword="false"/>；
    /// 未命中时，返回键名本身且 <see cref="LocalizedString.ResourceNotFound"/> 为 <see langword="true"/>
    /// </returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="name"/> 为 <see langword="null"/> 时抛出</exception>
    public LocalizedString this[string name]
    {
        get
        {
            Check.NotNull(name);
            var candidates = BuildCandidates();
            var hit = _snapshot.Find(_resourceName, name, candidates);

            return hit is not null
                ? new LocalizedString(name, hit.Value, resourceNotFound: false)
                : new LocalizedString(name, name, resourceNotFound: true);
        }
    }

    /// <summary>
    /// 指定名称对应的本地化字符串，并以 <paramref name="arguments"/> 格式化占位符
    /// </summary>
    /// <param name="name">文本键名</param>
    /// <param name="arguments">格式化参数，传递给 <see cref="string.Format(IFormatProvider, string, object[])"/></param>
    /// <returns>
    /// 当快照命中时，返回以 <paramref name="arguments"/> 格式化后的本地化字符串，
    /// <see cref="LocalizedString.ResourceNotFound"/> 为 <see langword="false"/>；
    /// 未命中时，直接返回键名本身（不执行格式化，避免键名含大括号时抛出 <see cref="FormatException"/>），
    /// <see cref="LocalizedString.ResourceNotFound"/> 为 <see langword="true"/>
    /// </returns>
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var baseString = this[name];
            if (baseString.ResourceNotFound)
            {
                return baseString;
            }

            var formatted = string.Format(CultureInfo.CurrentCulture, baseString.Value, arguments);
            return new LocalizedString(name, formatted, resourceNotFound: false);
        }
    }

    /// <summary>
    /// 返回当前资源在给定文化范围内的所有本地化字符串
    /// </summary>
    /// <param name="includeParentCultures">
    /// 为 <see langword="true"/> 时展开完整文化回退链（父级文化 + 默认文化）；
    /// 为 <see langword="false"/> 时仅查询当前文化
    /// </param>
    /// <returns>当前文化范围内的所有 <see cref="LocalizedString"/> 序列</returns>
    /// <remarks>
    /// 当 <paramref name="includeParentCultures"/> 为 <see langword="false"/> 时，结果仅限当前文化，
    /// 与 ASP.NET Core <c>ResourceManagerStringLocalizer</c> 的行为约定一致，
    /// <strong>不</strong>应用上下文的父级文化或默认文化回退；
    /// 当为 <see langword="true"/> 时，使用 <see cref="CultureFallback.Expand"/> 展开完整回退链，
    /// 包含父级文化和默认文化的全部条目。
    /// </remarks>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var context = _accessor.Current ?? new LocalizationContext(_options.DefaultCulture);

        IReadOnlyList<string> candidates = includeParentCultures
            ? CultureFallback.Expand(context, _options)
            : new[] { context.CultureName };

        var all = _snapshot.GetAll(_resourceName, candidates);
        return all.Select(kvp => new LocalizedString(kvp.Key, kvp.Value.Value, resourceNotFound: false));
    }

    // 构建当前请求的文化候选列表；上下文未设置时退回默认文化
    /// <summary>执行 BuildCandidates 操作</summary>
    /// <returns>BuildCandidates 的执行结果</returns>
    private IReadOnlyList<string> BuildCandidates()
    {
        var context = _accessor.Current ?? new LocalizationContext(_options.DefaultCulture);
        return CultureFallback.Expand(context, _options);
    }
}
