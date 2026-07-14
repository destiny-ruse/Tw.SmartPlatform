using Microsoft.Extensions.Localization;

namespace Tw.AspNetCore.Localization;

/// <summary>
/// 将资源类型绑定到静态快照字符串本地化器
/// </summary>
/// <typeparam name="TResource">标识本地化资源范围的类型</typeparam>
public sealed class StaticSnapshotStringLocalizer<TResource> : IStringLocalizer<TResource>
{
    /// <summary>
    /// 由本地化器工厂创建的资源专用实现
    /// </summary>
    private readonly IStringLocalizer _inner;

    /// <summary>
    /// 初始化指定资源类型的静态快照字符串本地化器
    /// </summary>
    /// <param name="factory">按资源类型创建本地化器的工厂</param>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> 为 <see langword="null"/> 时抛出</exception>
    public StaticSnapshotStringLocalizer(IStringLocalizerFactory factory)
    {
        Check.NotNull(factory);
        _inner = factory.Create(typeof(TResource));
    }

    /// <inheritdoc />
    public LocalizedString this[string name] => _inner[name];

    /// <inheritdoc />
    public LocalizedString this[string name, params object[] arguments] => _inner[name, arguments];

    /// <inheritdoc />
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        _inner.GetAllStrings(includeParentCultures);
}
