namespace Tw.Uow;

/// <summary>
/// 工作单元事务行为
/// </summary>
public enum UnitOfWorkTransactionBehavior
{
    /// <summary>
    /// 不启用事务
    /// </summary>
    NonTransactional,

    /// <summary>
    /// 启用事务
    /// </summary>
    Transactional
}
