using Tw.Localization.Requests;

namespace Tw.Localization.Json;

/// <summary>
/// 基于静态 JSON 资源的本地化文本贡献者，实现 <see cref="ITextResourceContributor"/>
/// </summary>
public sealed class JsonTextResourceContributor : ITextResourceContributor
{
    private readonly StaticTextSnapshot _snapshot;

    /// <summary>
    /// 初始化 <see cref="JsonTextResourceContributor"/> 类的新实例
    /// </summary>
    /// <param name="resources">已解析的 JSON 文本资源列表</param>
    /// <param name="priority">该贡献者在查找管道中的优先级；数值越大越优先执行</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="resources"/> 为 <see langword="null"/> 时抛出</exception>
    public JsonTextResourceContributor(IReadOnlyList<JsonTextResource> resources, int priority)
    {
        Check.NotNull(resources);
        _snapshot = new StaticTextSnapshot(resources);
        Priority = priority;
    }

    /// <inheritdoc />
    public int Priority { get; }

    /// <inheritdoc />
    public ValueTask<LocalizedText?> GetOrNullAsync(
        TextLookupRequest request,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<LocalizedText?>(
            _snapshot.Find(request.ResourceName, request.Name, request.CandidateCultureNames));
    }

    /// <inheritdoc />
    public ValueTask FillAsync(
        TextFillRequest request,
        IDictionary<string, LocalizedText> texts,
        CancellationToken cancellationToken = default)
    {
        // GetAll 内部已按低优先级在前、高优先级在后的顺序合并，直接写入目标字典；
        // 上层编排器中后执行的高优先级贡献者可继续覆盖这些条目
        foreach (var kv in _snapshot.GetAll(request.ResourceName, request.CandidateCultureNames))
        {
            texts[kv.Key] = kv.Value;
        }

        return ValueTask.CompletedTask;
    }
}
