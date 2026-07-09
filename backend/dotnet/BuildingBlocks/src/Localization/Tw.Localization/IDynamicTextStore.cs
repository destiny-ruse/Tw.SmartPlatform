using Tw.Localization.Requests;

namespace Tw.Localization;

/// <summary>
/// 提供对动态（运行时可写入）本地化文本存储的访问能力，
/// 支持单条查找和批量列表获取
/// </summary>
public interface IDynamicTextStore
{
    /// <summary>
    /// 尝试在动态存储中查找与请求匹配的单条本地化文本
    /// </summary>
    /// <param name="request">包含资源名称、键名、上下文及候选文化列表的查找请求</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌</param>
    /// <returns>找到时返回对应的 <see cref="LocalizedText"/>；否则返回 <see langword="null"/></returns>
    ValueTask<LocalizedText?> FindAsync(
        TextLookupRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取与批量填充请求匹配的所有动态本地化文本条目
    /// </summary>
    /// <param name="request">包含资源名称、上下文及候选文化列表的批量填充请求</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌</param>
    /// <returns>所有匹配的 <see cref="LocalizedText"/> 的只读列表</returns>
    ValueTask<IReadOnlyList<LocalizedText>> GetListAsync(
        TextFillRequest request,
        CancellationToken cancellationToken = default);
}
