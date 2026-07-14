namespace Tw.Data.Uow;

/// <summary>
/// 描述工作单元能否覆盖 Outbox 写入及其事务完成状态
/// </summary>
/// <remarks>
/// 成功提交、成功回滚或释放工作单元后，事务边界均不得继续写入 Outbox
/// 释放只关闭当前作用域与 Outbox 写入资格，不表示事务已经提交
/// </remarks>
public interface IOutboxTransactionBoundary
{
    /// <summary>
    /// 当前工作单元是否允许写入同一事务边界内的 Outbox
    /// </summary>
    /// <remarks>成功提交、成功回滚或释放后返回 <see langword="false"/></remarks>
    bool CanWriteOutbox { get; }

    /// <summary>
    /// 当前事务边界是否已经提交或回滚
    /// </summary>
    bool IsCompleted { get; }
}
