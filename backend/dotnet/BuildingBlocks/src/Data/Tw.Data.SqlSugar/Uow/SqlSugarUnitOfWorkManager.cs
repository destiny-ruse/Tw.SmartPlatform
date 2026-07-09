using Tw.Data.SqlSugar.Connection;
using Tw.Uow;

namespace Tw.Data.SqlSugar.Uow;

public sealed class SqlSugarUnitOfWorkManager(ISqlSugarClientFactory clientFactory) : IUnitOfWorkManager
{
    private readonly AsyncLocal<IUnitOfWork?> _current = new();

    public IUnitOfWork? Current => _current.Value;

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
