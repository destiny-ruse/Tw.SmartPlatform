using Tw.Data.SqlSugar.Connection;
using Tw.Uow;

namespace Tw.Data.SqlSugar.Uow;

/// <summary>
/// 封装SqlSugarUnitOfWorkManager相关的数据和行为
/// </summary>
public sealed class SqlSugarUnitOfWorkManager(ISqlSugarClientFactory clientFactory) : IUnitOfWorkManager
{
    /// <summary>
    /// 保存当前类型处理流程依赖的current
    /// </summary>
    private readonly AsyncLocal<IUnitOfWork?> _current = new();

    /// <summary>
    /// Current在当前对象中的业务含义
    /// </summary>
    public IUnitOfWork? Current => _current.Value;

    /// <summary>
    /// 开始测试事务并返回事务上下文
    /// </summary>
    /// <param name="options">用于配置当前组件行为的选项</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的IUnitOfWork</returns>
    public Task<IUnitOfWork> BeginAsync(UnitOfWorkOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

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
