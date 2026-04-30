namespace Tw.DependencyInjection.Cancellation;

/// <summary>
/// 基于 <see cref="AsyncLocal{T}"/> 的 ambient 取消令牌实现。
/// 同时实现 <see cref="ICancellationTokenProvider"/> 和 <see cref="ICurrentCancellationTokenAccessor"/>。
/// </summary>
public sealed class CurrentCancellationTokenAccessor : ICancellationTokenProvider, ICurrentCancellationTokenAccessor
{
    private static readonly AsyncLocal<CancellationToken?> Ambient = new();

    /// <inheritdoc />
    public CancellationToken Token => Ambient.Value ?? CancellationToken.None;

    /// <summary>
    /// 设置 ambient token 至 <paramref name="token"/>，Dispose 时恢复原值。
    /// 应以 LIFO 顺序 Dispose（即正常 <c>using</c> 语句顺序）；非 LIFO 顺序 Dispose 行为未定义。
    /// </summary>
    public IDisposable Use(CancellationToken token)
    {
        var previous = Ambient.Value;
        Ambient.Value = token;
        return new RestoreScope(previous);
    }

    private sealed class RestoreScope : IDisposable
    {
        private readonly CancellationToken? _previous;
        private bool _disposed;

        public RestoreScope(CancellationToken? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Ambient.Value = _previous;
        }
    }
}
