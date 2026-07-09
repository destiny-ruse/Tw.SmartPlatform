namespace Tw.Data.Auditing;

/// <summary>定义 IAuditedEntity 契约</summary>
public interface IAuditedEntity
{
    /// <summary>表示 CreatedAt 属性</summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>表示 UpdatedAt 属性</summary>
    DateTimeOffset UpdatedAt { get; set; }

    /// <summary>表示 CreatedBy 属性</summary>
    string? CreatedBy { get; set; }

    /// <summary>表示 UpdatedBy 属性</summary>
    string? UpdatedBy { get; set; }
}
