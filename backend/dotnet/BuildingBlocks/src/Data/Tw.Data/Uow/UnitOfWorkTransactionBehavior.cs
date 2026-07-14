namespace Tw.Data.Uow;

/// <summary>
/// 工作单元的数据事务行为
/// </summary>
public enum UnitOfWorkTransactionBehavior
{
    /// <summary>
    /// 不创建数据事务
    /// </summary>
    NonTransactional,

    /// <summary>
    /// 在数据事务中执行工作单元
    /// </summary>
    Transactional
}
