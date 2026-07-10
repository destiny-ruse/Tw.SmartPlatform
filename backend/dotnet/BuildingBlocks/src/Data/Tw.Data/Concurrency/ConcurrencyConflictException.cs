namespace Tw.Data.Concurrency;

/// <summary>
/// 封装ConcurrencyConflict异常相关的数据和行为
/// </summary>
public sealed class ConcurrencyConflictException(string resourceType, string resourceId)
    : Exception("Data has been changed by another request.")
{
    /// <summary>
    /// 代码在当前对象中的业务含义
    /// </summary>
    public string Code { get; } = "DATA:CONFLICT";

    /// <summary>
    /// 资源类型在当前对象中的业务含义
    /// </summary>
    public string ResourceType { get; } = resourceType;

    /// <summary>
    /// 资源标识在当前对象中的业务含义
    /// </summary>
    public string ResourceId { get; } = resourceId;
}
