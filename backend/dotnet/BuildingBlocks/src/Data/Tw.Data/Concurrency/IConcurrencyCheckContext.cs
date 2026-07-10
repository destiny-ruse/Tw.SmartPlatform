namespace Tw.Data.Concurrency;

/// <summary>
/// 定义ConcurrencyCheck上下文的能力边界
/// </summary>
public interface IConcurrencyCheckContext
{
    /// <summary>
    /// 资源类型在当前对象中的业务含义
    /// </summary>
    string ResourceType { get; }

    /// <summary>
    /// 资源标识在当前对象中的业务含义
    /// </summary>
    string ResourceId { get; }

    /// <summary>
    /// ExpectedConcurrencyStamp在当前对象中的业务含义
    /// </summary>
    string? ExpectedConcurrencyStamp { get; }

    /// <summary>
    /// ExpectedVersionStamp在当前对象中的业务含义
    /// </summary>
    long? ExpectedVersionStamp { get; }
}
