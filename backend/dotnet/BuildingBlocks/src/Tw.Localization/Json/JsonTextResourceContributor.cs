using Tw.Localization.Requests;

namespace Tw.Localization.Json;

/// <summary>
/// 基于静态 JSON 资源的本地化文本贡献者，实现 <see cref="ITextResourceContributor"/>
/// </summary>
public sealed class JsonTextResourceContributor : ITextResourceContributor
{
    private readonly IReadOnlyList<JsonTextResource> _resources;

    /// <summary>
    /// 初始化 <see cref="JsonTextResourceContributor"/> 类的新实例
    /// </summary>
    /// <param name="resources">已解析的 JSON 文本资源列表</param>
    /// <param name="priority">该贡献者在查找管道中的优先级；数值越大越优先执行</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="resources"/> 为 <see langword="null"/> 时抛出</exception>
    public JsonTextResourceContributor(IReadOnlyList<JsonTextResource> resources, int priority)
    {
        _resources = Check.NotNull(resources);
        Priority = priority;
    }

    /// <inheritdoc />
    public int Priority { get; }

    /// <inheritdoc />
    public ValueTask<LocalizedText?> GetOrNullAsync(
        TextLookupRequest request,
        CancellationToken cancellationToken = default)
    {
        // 按候选文化顺序（高优先级在前）逐个查找，首次命中即返回
        foreach (var culture in request.CandidateCultureNames)
        {
            foreach (var resource in _resources)
            {
                if (!string.Equals(resource.ResourceName, request.ResourceName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.Equals(resource.CultureName, culture, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (resource.Texts.TryGetValue(request.Name, out var value))
                {
                    var result = new LocalizedText(
                        request.ResourceName,
                        request.Name,
                        value,
                        culture,
                        false,
                        LocalizedTextSource.StaticJson);

                    return new ValueTask<LocalizedText?>(result);
                }
            }
        }

        return new ValueTask<LocalizedText?>((LocalizedText?)null);
    }

    /// <inheritdoc />
    public ValueTask FillAsync(
        TextFillRequest request,
        IDictionary<string, LocalizedText> texts,
        CancellationToken cancellationToken = default)
    {
        // 以低优先级（后备文化）在前、高优先级（当前文化）在后的顺序填充，
        // 确保高优先级的值能覆盖低优先级的值
        for (var i = request.CandidateCultureNames.Count - 1; i >= 0; i--)
        {
            var culture = request.CandidateCultureNames[i];

            foreach (var resource in _resources)
            {
                if (!string.Equals(resource.ResourceName, request.ResourceName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.Equals(resource.CultureName, culture, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var (key, value) in resource.Texts)
                {
                    texts[key] = new LocalizedText(
                        request.ResourceName,
                        key,
                        value,
                        culture,
                        false,
                        LocalizedTextSource.StaticJson);
                }
            }
        }

        return ValueTask.CompletedTask;
    }
}
