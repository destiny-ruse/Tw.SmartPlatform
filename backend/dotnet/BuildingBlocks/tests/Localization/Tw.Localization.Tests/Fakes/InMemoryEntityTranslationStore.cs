using Tw.Localization.Requests;

namespace Tw.Localization.Tests.Fakes;

/// <summary>
/// 基于内存的实体翻译存储 Fake，用于单元测试中模拟 <see cref="IEntityTranslationStore"/>
/// </summary>
internal sealed class InMemoryEntityTranslationStore : IEntityTranslationStore
{
    private readonly List<EntityTranslation> _translations = [];

    /// <summary>
    /// 获取 <see cref="GetListAsync"/> 的累计调用次数
    /// </summary>
    public int GetListCallCount { get; private set; }

    /// <summary>
    /// 向存储中添加一条实体翻译记录
    /// </summary>
    /// <param name="translation">要添加的实体翻译</param>
    public void Add(EntityTranslation translation)
    {
        _translations.Add(translation);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<EntityTranslation>> GetListAsync(EntityTranslationQuery query, CancellationToken cancellationToken = default)
    {
        GetListCallCount++;

        var matchingCultures = new HashSet<string>(query.CandidateCultureNames, StringComparer.OrdinalIgnoreCase);

        // 构建请求键的快速查找集合
        var requestedKeys = new HashSet<(string EntityType, string EntityId, string FieldName)>(
            query.Keys.Select(k => (k.EntityType, k.EntityId, k.FieldName)));

        var results = _translations
            .Where(t =>
                requestedKeys.Contains((t.EntityType, t.EntityId, t.FieldName)) &&
                matchingCultures.Contains(t.CultureName) &&
                (t.TenantId == query.Context.TenantId || t.TenantId is null))
            .ToList();

        return new ValueTask<IReadOnlyList<EntityTranslation>>(results);
    }
}
