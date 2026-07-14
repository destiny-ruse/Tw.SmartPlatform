using Tw.Data.SqlSugar.Connection;
using Tw.Data.Uow;

namespace Tw.Data.SqlSugar.Uow;

/// <summary>
/// 通过 SqlSugar 客户端创建工作单元并协调当前异步调用链中的活动作用域
/// </summary>
/// <param name="clientFactory">为新工作单元创建 SqlSugar 客户端的工厂</param>
public sealed class SqlSugarUnitOfWorkCoordinator(ISqlSugarClientFactory clientFactory) : IUnitOfWorkCoordinator
{
    /// <summary>
    /// 保存当前异步调用链中的活动工作单元
    /// </summary>
    private readonly AsyncLocal<IUnitOfWork?> _current = new();

    /// <summary>
    /// 当前异步调用链中的活动工作单元
    /// </summary>
    public IUnitOfWork? Current => _current.Value;

    /// <summary>
    /// 按指定作用域创建或复用 SqlSugar 工作单元
    /// </summary>
    /// <param name="options">工作单元作用域和事务行为</param>
    /// <param name="cancellationToken">创建 SqlSugar 客户端时使用的取消令牌</param>
    /// <returns>新建或按作用域规则复用的工作单元</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> 为 <see langword="null"/></exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> 已请求取消</exception>
    public Task<IUnitOfWork> BeginAsync(
        UnitOfWorkOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        if (options.Scope == UnitOfWorkScope.Required && _current.Value is not null)
        {
            return Task.FromResult(_current.Value);
        }

        var previous = _current.Value;
        var unitOfWork = new SqlSugarUnitOfWork(clientFactory, () => _current.Value = previous, cancellationToken);
        _current.Value = unitOfWork;
        return Task.FromResult<IUnitOfWork>(unitOfWork);
    }
}
