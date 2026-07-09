namespace Tw.Threading;

/// <summary>
/// 使用 <see cref="AsyncLocal{T}"/> 维护异步调用链内的取消令牌作用域
/// </summary>
/// <remarks>
/// 作用域随 async/await 在同一执行链内传播；释放作用域时恢复外层作用域。
/// 该类型不保存静态全局可变业务状态，也不跨独立异步执行链传播令牌。
/// </remarks>
public sealed class AsyncLocalCancellationTokenScopeProvider
{
    private readonly AsyncLocal<CancellationTokenOverride?> _current = new();

    /// <summary>
    /// 当前作用域的取消令牌覆盖值
    /// </summary>
    /// <value>没有活动作用域时为 <see langword="null"/></value>
    public CancellationTokenOverride? Current => _current.Value;

    /// <summary>
    /// 建立一个新的取消令牌作用域
    /// </summary>
    /// <param name="cancellationToken">作用域内生效的取消令牌</param>
    /// <returns>释放后恢复外层作用域的 <see cref="IDisposable"/></returns>
    public IDisposable BeginScope(CancellationToken cancellationToken)
    {
        var previous = _current.Value;
        _current.Value = new CancellationTokenOverride(cancellationToken);
        return new CancellationTokenScope(this, previous);
    }

    private sealed class CancellationTokenScope(
        AsyncLocalCancellationTokenScopeProvider provider,
        CancellationTokenOverride? previous) : IDisposable
    {
        private AsyncLocalCancellationTokenScopeProvider? providerRef = provider;

        public void Dispose()
        {
            var currentProvider = Interlocked.Exchange(ref providerRef, null);
            if (currentProvider is not null)
            {
                currentProvider._current.Value = previous;
            }
        }
    }
}
