using Tw.Localization.Requests;

namespace Tw.Localization.Tests.Fakes;

/// <summary>
/// 基于内存的动态文本存储 Fake，用于单元测试中模拟 <see cref="IDynamicTextStore"/>
/// </summary>
internal sealed class InMemoryDynamicTextStore : IDynamicTextStore
{
    // 键：(tenantId or null, resourceName, cultureName, name)
    /// <summary>表示 _records 字段</summary>
    private readonly List<(string? TenantId, LocalizedText Text)> _records = [];

    /// <summary>
    /// <see cref="GetListAsync"/> 的累计调用次数
    /// </summary>
    public int GetListCallCount { get; private set; }

    /// <summary>
    /// 向存储中添加一条本地化文本记录
    /// </summary>
    /// <param name="text">要添加的本地化文本</param>
    /// <param name="tenantId">关联的租户标识；为 <see langword="null"/> 时表示全局记录</param>
    public void Add(LocalizedText text, string? tenantId = null)
    {
        _records.Add((tenantId, text));
    }

    /// <inheritdoc />
    public ValueTask<LocalizedText?> FindAsync(TextLookupRequest request, CancellationToken cancellationToken = default)
    {
        LocalizedText? result = null;

        foreach (var culture in request.CandidateCultureNames)
        {
            // 优先当前租户，其次全局
            var tenantMatch = _records.FirstOrDefault(r =>
                r.TenantId == request.Context.TenantId &&
                r.Text.ResourceName == request.ResourceName &&
                r.Text.Name == request.Name &&
                string.Equals(r.Text.CultureName, culture, StringComparison.OrdinalIgnoreCase));

            if (tenantMatch.Text is not null)
            {
                result = tenantMatch.Text;
                break;
            }

            var globalMatch = _records.FirstOrDefault(r =>
                r.TenantId is null &&
                r.Text.ResourceName == request.ResourceName &&
                r.Text.Name == request.Name &&
                string.Equals(r.Text.CultureName, culture, StringComparison.OrdinalIgnoreCase));

            if (globalMatch.Text is not null)
            {
                result = globalMatch.Text;
                break;
            }
        }

        return new ValueTask<LocalizedText?>(result);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<LocalizedText>> GetListAsync(TextFillRequest request, CancellationToken cancellationToken = default)
    {
        GetListCallCount++;

        var matchingCultures = new HashSet<string>(request.CandidateCultureNames, StringComparer.OrdinalIgnoreCase);

        var results = _records
            .Where(r =>
                r.Text.ResourceName == request.ResourceName &&
                matchingCultures.Contains(r.Text.CultureName) &&
                (r.TenantId == request.Context.TenantId || r.TenantId is null))
            .Select(r => r.Text)
            .ToList();

        return new ValueTask<IReadOnlyList<LocalizedText>>(results);
    }
}
