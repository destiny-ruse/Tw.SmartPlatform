namespace Tw.EventBus.Cap.Storage;

/// <summary>表示 SqlSugarCapStorageOptions 类型</summary>
public sealed class SqlSugarCapStorageOptions
{
    /// <summary>表示 ConnectionName 属性</summary>
    public string? ConnectionName { get; set; }

    /// <summary>表示 Schema 属性</summary>
    public string Schema { get; set; } = "cap";

    /// <summary>表示 PublishedTable 属性</summary>
    public string PublishedTable { get; set; } = "published";

    /// <summary>表示 ReceivedTable 属性</summary>
    public string ReceivedTable { get; set; } = "received";

    /// <summary>表示 LockTable 属性</summary>
    public string LockTable { get; set; } = "locks";

    /// <summary>执行 Validate 操作</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionName))
        {
            throw new InvalidOperationException("CAP SqlSugar connection name is required");
        }
    }
}
