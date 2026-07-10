namespace Tw.Data.Auditing;

/// <summary>
/// 定义AuditedEntity的能力边界
/// </summary>
public interface IAuditedEntity
{
    /// <summary>
    /// CreatedAt在当前对象中的业务含义
    /// </summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// UpdatedAt在当前对象中的业务含义
    /// </summary>
    DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// CreatedBy在当前对象中的业务含义
    /// </summary>
    string? CreatedBy { get; set; }

    /// <summary>
    /// UpdatedBy在当前对象中的业务含义
    /// </summary>
    string? UpdatedBy { get; set; }
}
