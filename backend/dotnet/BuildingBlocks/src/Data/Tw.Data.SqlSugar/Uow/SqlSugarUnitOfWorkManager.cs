using Tw.Data.SqlSugar.Connection;
using Tw.Uow;

namespace Tw.Data.SqlSugar.Uow;

/// <summary>表示 SqlSugarUnitOfWorkManager 类型</summary>
public sealed class SqlSugarUnitOfWorkManager(ISqlSugarClientFactory clientFactory) : IUnitOfWorkManager
{
    /// <summary>表示 _current 字段</summary>
    private readonly AsyncLocal<IUnitOfWork?> _current = new();

    /// <summary>表示 Current 属性</summary>
    public IUnitOfWork? Current => _current.Value;

    /// <summary>执行 BeginAsync 操作</summary>
    /// <param name="options">options 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>BeginAsync 的执行结果</returns>
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
