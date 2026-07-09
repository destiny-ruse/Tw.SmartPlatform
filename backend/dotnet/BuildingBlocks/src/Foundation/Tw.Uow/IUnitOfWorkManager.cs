namespace Tw.Uow;

/// <summary>
/// 工作单元管理器，负责创建和暴露当前工作单元
/// </summary>
public interface IUnitOfWorkManager
{
    /// <summary>
    /// 当前异步调用链中的工作单元
    /// </summary>
    IUnitOfWork? Current { get; }

    /// <summary>
    /// 开始一个工作单元
    /// </summary>
    /// <param name="options">工作单元创建选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新建或复用的工作单元</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="options"/> 为 <see langword="null"/> 时抛出</exception>
    Task<IUnitOfWork> BeginAsync(
        UnitOfWorkOptions options,
        CancellationToken cancellationToken = default);
}
