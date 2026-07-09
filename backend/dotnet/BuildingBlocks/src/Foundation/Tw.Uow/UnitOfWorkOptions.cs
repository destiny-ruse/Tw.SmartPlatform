namespace Tw.Uow;

/// <summary>
/// 工作单元作用域行为
/// </summary>
public enum UnitOfWorkScope
{
    /// <summary>
    /// 复用当前工作单元；不存在时创建新工作单元
    /// </summary>
    Required,

    /// <summary>
    /// 始终创建新的工作单元
    /// </summary>
    RequiresNew,

    /// <summary>
    /// 暂停当前工作单元
    /// </summary>
    Suppress
}

/// <summary>
/// 工作单元创建选项
/// </summary>
/// <param name="Scope">工作单元作用域行为</param>
/// <param name="TransactionBehavior">工作单元事务行为</param>
public sealed record UnitOfWorkOptions(
    UnitOfWorkScope Scope,
    UnitOfWorkTransactionBehavior TransactionBehavior)
{
    /// <summary>
    /// 默认工作单元选项，使用 Required 作用域和事务性行为
    /// </summary>
    public static UnitOfWorkOptions Default { get; } = new(
        UnitOfWorkScope.Required,
        UnitOfWorkTransactionBehavior.Transactional);
}
