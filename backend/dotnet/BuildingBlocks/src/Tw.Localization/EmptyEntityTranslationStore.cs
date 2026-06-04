using Tw.Localization.Requests;

namespace Tw.Localization;

/// <summary>
/// 默认空实体翻译存储，始终返回空列表，作为业务应用提供真实存储之前的占位默认实现。
/// 业务应用注册自己的 <see cref="IEntityTranslationStore"/> 实现后，此默认实现将被覆盖
/// </summary>
internal sealed class EmptyEntityTranslationStore : IEntityTranslationStore
{
    /// <inheritdoc />
    public ValueTask<IReadOnlyList<EntityTranslation>> GetListAsync(
        EntityTranslationQuery query,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<IReadOnlyList<EntityTranslation>>(Array.Empty<EntityTranslation>());
    }
}
