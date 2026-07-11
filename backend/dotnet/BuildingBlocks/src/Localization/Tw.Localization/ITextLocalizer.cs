namespace Tw.Localization;

/// <summary>
/// 提供面向调用方的高层文本本地化查找能力，
/// 屏蔽底层资源贡献者与缓存细节
/// </summary>
public interface ITextLocalizer
{
    /// <summary>
    /// 在指定资源集合中查找单条本地化文本
    /// </summary>
    /// <param name="resourceName">资源集合的名称，例如 "App"</param>
    /// <param name="name">文本条目的键名，例如 "Menu.Home"</param>
    /// <param name="context">本次查找使用的本地化上下文，包含目标文化和租户信息</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌</param>
    /// <returns>已解析的 <see cref="LocalizedText"/>；未找到时返回 <see cref="LocalizedText.ResourceNotFound"/> 为 <see langword="true"/> 的实例</returns>
    ValueTask<LocalizedText> GetAsync(
        string resourceName,
        string name,
        LocalizationContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// 获取指定资源集合在当前上下文下的所有本地化文本条目
    /// </summary>
    /// <param name="resourceName">资源集合的名称，例如 "App"</param>
    /// <param name="context">本次查找使用的本地化上下文，包含目标文化和租户信息</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌</param>
    /// <returns>该资源集合下所有已解析的 <see cref="LocalizedText"/> 的只读列表</returns>
    ValueTask<IReadOnlyList<LocalizedText>> GetAllAsync(
        string resourceName,
        LocalizationContext context,
        CancellationToken cancellationToken);
}
