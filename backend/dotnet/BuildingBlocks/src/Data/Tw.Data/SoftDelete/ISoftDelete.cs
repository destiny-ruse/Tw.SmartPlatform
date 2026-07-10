namespace Tw.Data.SoftDelete;

/// <summary>
/// 定义Soft删除的能力边界
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// sDeleted在当前对象中的业务含义
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// DeletedAt在当前对象中的业务含义
    /// </summary>
    DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// DeletedBy在当前对象中的业务含义
    /// </summary>
    string? DeletedBy { get; set; }
}
