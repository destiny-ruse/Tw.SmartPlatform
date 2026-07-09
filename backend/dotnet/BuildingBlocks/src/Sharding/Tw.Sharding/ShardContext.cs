using Tw.Sharding.Abstractions;

namespace Tw.Sharding;

/// <summary>表示 ShardContext 类型</summary>
public sealed class ShardContext : IShardContext
{
    /// <summary>表示 _current 字段</summary>
    private readonly AsyncLocal<ShardDescriptor?> _current = new();

    /// <summary>表示 Current 属性</summary>
    public ShardDescriptor Current => _current.Value ?? ShardDescriptor.None;

    /// <summary>执行 Change 操作</summary>
    /// <param name="descriptor">descriptor 参数</param>
    /// <returns>Change 的执行结果</returns>
    public IDisposable Change(ShardDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var previous = _current.Value;
        _current.Value = descriptor;
        return new RestoreScope(() => _current.Value = previous);
    }

    /// <summary>表示 RestoreScope 类型</summary>
    private sealed class RestoreScope(Action restore) : IDisposable
    {
        /// <summary>表示 _disposed 字段</summary>
        private bool _disposed;

        /// <summary>执行 Dispose 操作</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            restore();
            _disposed = true;
        }
    }
}
