namespace Tw.Localization.Caching;

/// <summary>
/// 提供本地化缓存的主动失效能力，支持按资源或按实体类型精准清除缓存条目
/// </summary>
public interface ILocalizationCacheInvalidator
{
    /// <summary>
    /// 使指定资源集合的缓存失效
    /// </summary>
    /// <param name="resourceName">要失效的资源集合名称，例如 "App"</param>
    /// <param name="cultureName">要失效的 BCP 47 文化名称；为 <see langword="null"/> 时使所有文化失效</param>
    /// <param name="tenantId">要失效的租户标识；为 <see langword="null"/> 时使所有租户失效</param>
    void InvalidateResource(string resourceName, string? cultureName = null, string? tenantId = null);

    /// <summary>
    /// 使指定实体类型的翻译缓存失效
    /// </summary>
    /// <param name="entityType">要失效的实体类型名称，例如 "Product"</param>
    /// <param name="entityId">要失效的实体标识；为 <see langword="null"/> 时使该类型下所有实体失效</param>
    /// <param name="cultureName">要失效的 BCP 47 文化名称；为 <see langword="null"/> 时使所有文化失效</param>
    /// <param name="tenantId">要失效的租户标识；为 <see langword="null"/> 时使所有租户失效</param>
    void InvalidateEntity(string entityType, string? entityId = null, string? cultureName = null, string? tenantId = null);
}
