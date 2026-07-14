namespace Tw.Data.Uow;

/// <summary>
/// 描述工作单元能否覆盖 Outbox 写入及其事务完成状态
/// </summary>
public interface IOutboxTransactionBoundary
{
    /// <summary>
    /// 当前工作单元是否允许写入同一事务边界内的 Outbox
    /// </summary>
    bool CanWriteOutbox { get; }

    /// <summary>
    /// 当前事务边界是否已经提交或回滚
    /// </summary>
    bool IsCompleted { get; }
}
