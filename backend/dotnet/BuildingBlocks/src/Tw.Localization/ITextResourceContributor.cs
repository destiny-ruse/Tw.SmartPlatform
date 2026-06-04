using Tw.Localization.Requests;

namespace Tw.Localization;

/// <summary>
/// 表示一个本地化文本资源贡献者，可按优先级向查找管道提供或填充文本条目
/// </summary>
public interface ITextResourceContributor
{
    /// <summary>
    /// 贡献者在查找管道中的优先级；数值越大越优先执行
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 尝试为单条文本查找请求返回一个本地化文本；若该贡献者无法提供则返回 <see langword="null"/>
    /// </summary>
    /// <param name="request">包含资源名称、键名、上下文及候选文化列表的查找请求</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌</param>
    /// <returns>找到时返回对应的 <see cref="LocalizedText"/>；否则返回 <see langword="null"/></returns>
    ValueTask<LocalizedText?> GetOrNullAsync(
        TextLookupRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 将该贡献者持有的本地化文本批量填充到 <paramref name="texts"/> 字典中；
    /// 已存在的键不应被覆盖
    /// </summary>
    /// <param name="request">包含资源名称、上下文及候选文化列表的批量填充请求</param>
    /// <param name="texts">目标字典，键为文本条目键名，值为已解析的 <see cref="LocalizedText"/></param>
    /// <param name="cancellationToken">用于取消异步操作的令牌</param>
    ValueTask FillAsync(
        TextFillRequest request,
        IDictionary<string, LocalizedText> texts,
        CancellationToken cancellationToken = default);
}
