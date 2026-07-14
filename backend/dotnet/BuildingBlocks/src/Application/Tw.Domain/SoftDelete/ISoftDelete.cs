namespace Tw.Domain.SoftDelete;

/// <summary>
/// 标记通过逻辑删除保留历史状态的领域实体
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// 实体是否已逻辑删除
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// 实体逻辑删除时间
    /// </summary>
    DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// 执行逻辑删除的主体标识
    /// </summary>
    string? DeletedBy { get; set; }
}
