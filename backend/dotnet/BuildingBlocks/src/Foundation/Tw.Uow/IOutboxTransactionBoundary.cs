namespace Tw.Uow;

/// <summary>
/// 定义OutboxTransaction边界的能力边界
/// </summary>
public interface IOutboxTransactionBoundary
{
    /// <summary>
    /// CanWriteOutbox在当前对象中的业务含义
    /// </summary>
    bool CanWriteOutbox { get; }

    /// <summary>
    /// sCompleted在当前对象中的业务含义
    /// </summary>
    bool IsCompleted { get; }
}
