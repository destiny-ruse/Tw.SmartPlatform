namespace Tw.Data.Uow;

/// <summary>
/// 工作单元与当前异步调用链的作用域关系
/// </summary>
public enum UnitOfWorkScope
{
    /// <summary>
    /// 复用当前工作单元，不存在时创建新工作单元
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
/// 指定工作单元的作用域和事务行为
/// </summary>
/// <param name="Scope">工作单元与当前异步调用链的作用域关系</param>
/// <param name="TransactionBehavior">工作单元是否启用事务</param>
public sealed record UnitOfWorkOptions(
    UnitOfWorkScope Scope,
    UnitOfWorkTransactionBehavior TransactionBehavior)
{
    /// <summary>
    /// 复用当前作用域并启用事务的默认选项
    /// </summary>
    public static UnitOfWorkOptions Default { get; } = new(
        UnitOfWorkScope.Required,
        UnitOfWorkTransactionBehavior.Transactional);
}
