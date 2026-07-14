namespace Tw.Domain.Auditing;

/// <summary>
/// 为领域实体提供创建和更新审计信息
/// </summary>
public interface IAuditedEntity
{
    /// <summary>
    /// 实体创建时间
    /// </summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 实体最后更新时间
    /// </summary>
    DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// 创建实体的主体标识
    /// </summary>
    string? CreatedBy { get; set; }

    /// <summary>
    /// 最后更新实体的主体标识
    /// </summary>
    string? UpdatedBy { get; set; }
}
