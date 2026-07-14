namespace Tw.Sharding;

/// <summary>
/// 在异步调用链中保存并恢复当前分片描述
/// </summary>
public sealed class ShardContext : IShardContext
{
    /// <summary>
    /// 当前异步调用链显式选择的分片描述
    /// </summary>
    private readonly AsyncLocal<ShardDescriptor?> _current = new();

    /// <summary>
    /// 当前作用域的分片描述；未指定时为空分片描述
    /// </summary>
    public ShardDescriptor Current => _current.Value ?? ShardDescriptor.None;

    /// <summary>
    /// 在新作用域内切换分片描述，并在作用域释放时恢复先前描述
    /// </summary>
    /// <param name="descriptor">新作用域使用的分片描述</param>
    /// <returns>负责恢复先前分片描述的作用域</returns>
    /// <exception cref="ArgumentNullException">descriptor 为 null 时抛出</exception>
    public IDisposable Change(ShardDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var previous = _current.Value;
        _current.Value = descriptor;
        return new RestoreScope(() => _current.Value = previous);
    }

    /// <summary>
    /// 释放时恢复先前分片描述的幂等作用域
    /// </summary>
    /// <param name="restore">首次释放作用域时调用的恢复操作</param>
    private sealed class RestoreScope(Action restore) : IDisposable
    {
        /// <summary>
        /// 当前执行上下文是否已经执行恢复操作
        /// </summary>
        private readonly AsyncLocal<bool> _disposed = new();

        /// <summary>
        /// 首次调用时恢复先前分片描述，后续调用不产生副作用
        /// </summary>
        public void Dispose()
        {
            if (_disposed.Value)
            {
                return;
            }

            restore();
            _disposed.Value = true;
        }
    }
}
