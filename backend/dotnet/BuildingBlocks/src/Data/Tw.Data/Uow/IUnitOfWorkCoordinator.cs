namespace Tw.Data.Uow;

/// <summary>
/// 创建工作单元并协调当前异步调用链中的活动作用域
/// </summary>
public interface IUnitOfWorkCoordinator
{
    /// <summary>
    /// 当前异步调用链中的活动工作单元
    /// </summary>
    IUnitOfWork? Current { get; }

    /// <summary>
    /// 按指定作用域和事务行为开始工作单元
    /// </summary>
    /// <param name="options">工作单元作用域和事务行为</param>
    /// <param name="cancellationToken">创建工作单元时使用的取消令牌</param>
    /// <returns>新建或按作用域规则复用的工作单元</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> 为 <see langword="null"/></exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> 已请求取消</exception>
    Task<IUnitOfWork> BeginAsync(
        UnitOfWorkOptions options,
        CancellationToken cancellationToken = default);
}
