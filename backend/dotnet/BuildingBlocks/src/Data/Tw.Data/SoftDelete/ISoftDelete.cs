namespace Tw.Data.SoftDelete;

/// <summary>定义 ISoftDelete 契约</summary>
public interface ISoftDelete
{
    /// <summary>表示 IsDeleted 属性</summary>
    bool IsDeleted { get; set; }

    /// <summary>表示 DeletedAt 属性</summary>
    DateTimeOffset? DeletedAt { get; set; }

    /// <summary>表示 DeletedBy 属性</summary>
    string? DeletedBy { get; set; }
}
