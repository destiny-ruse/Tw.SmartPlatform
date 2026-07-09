namespace Tw.Uow;

/// <summary>定义 IOutboxTransactionBoundary 契约</summary>
public interface IOutboxTransactionBoundary
{
    /// <summary>表示 CanWriteOutbox 属性</summary>
    bool CanWriteOutbox { get; }

    /// <summary>表示 IsCompleted 属性</summary>
    bool IsCompleted { get; }
}
