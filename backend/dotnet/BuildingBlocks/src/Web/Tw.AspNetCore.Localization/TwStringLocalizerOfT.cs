using Microsoft.Extensions.Localization;

namespace Tw.AspNetCore.Localization;

/// <summary>
/// <see cref="IStringLocalizer{TResource}"/> 的实现，将泛型资源类型转发给
/// <see cref="IStringLocalizerFactory"/> 创建的具体 <see cref="IStringLocalizer"/>。
/// </summary>
/// <remarks>
/// 遵循 Microsoft 标准 <c>StringLocalizer&lt;T&gt;</c> 模式：资源名称默认为 <typeparamref name="TResource"/>
/// 的简单类型名，由工厂负责解析。本类仅依赖静态快照，动态覆盖不在同步接口范围内。
/// </remarks>
/// <typeparam name="TResource">用于标识本地化资源范围的标记类型</typeparam>
public sealed class TwStringLocalizer<TResource> : IStringLocalizer<TResource>
{
    /// <summary>
    /// 保存当前类型处理流程依赖的inner
    /// </summary>
    private readonly IStringLocalizer _inner;

    /// <summary>
    /// 初始化 <see cref="TwStringLocalizer{TResource}"/> 类的新实例，
    /// 通过 <paramref name="factory"/> 为 <typeparamref name="TResource"/> 创建内部本地化器
    /// </summary>
    /// <param name="factory">用于创建内部 <see cref="IStringLocalizer"/> 实例的工厂</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="factory"/> 为 <see langword="null"/> 时抛出</exception>
    public TwStringLocalizer(IStringLocalizerFactory factory)
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
