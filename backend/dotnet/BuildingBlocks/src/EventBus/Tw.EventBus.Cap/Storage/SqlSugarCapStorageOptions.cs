namespace Tw.EventBus.Cap.Storage;

/// <summary>
/// 配置SqlSugarCapStorage的运行行为
/// </summary>
public sealed class SqlSugarCapStorageOptions
{
    /// <summary>
    /// Connection名称在当前对象中的业务含义
    /// </summary>
    public string? ConnectionName { get; set; }

    /// <summary>
    /// 架构在当前对象中的业务含义
    /// </summary>
    public string Schema { get; set; } = "cap";

    /// <summary>
    /// PublishedTable在当前对象中的业务含义
    /// </summary>
    public string PublishedTable { get; set; } = "published";

    /// <summary>
    /// ReceivedTable在当前对象中的业务含义
    /// </summary>
    public string ReceivedTable { get; set; } = "received";

    /// <summary>
    /// LockTable在当前对象中的业务含义
    /// </summary>
    public string LockTable { get; set; } = "locks";

    /// <summary>
    /// 校验当前配置或输入约束，并在非法时抛出异常
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionName))
        {
            throw new InvalidOperationException("CAP SqlSugar connection name is required");
        }
    }
}
