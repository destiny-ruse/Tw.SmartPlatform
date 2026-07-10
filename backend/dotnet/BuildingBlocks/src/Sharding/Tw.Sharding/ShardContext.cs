using Tw.Sharding.Abstractions;

namespace Tw.Sharding;

/// <summary>
/// 封装Shard上下文相关的数据和行为
/// </summary>
public sealed class ShardContext : IShardContext
{
    /// <summary>
    /// 保存当前类型处理流程依赖的current
    /// </summary>
    private readonly AsyncLocal<ShardDescriptor?> _current = new();

    /// <summary>
    /// Current在当前对象中的业务含义
    /// </summary>
    public ShardDescriptor Current => _current.Value ?? ShardDescriptor.None;

    /// <summary>
    /// 说明Change在当前类型中的职责
    /// </summary>
    /// <param name="descriptor">用于提供描述符</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    public IDisposable Change(ShardDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var previous = _current.Value;
        _current.Value = descriptor;
        return new RestoreScope(() => _current.Value = previous);
    }

    /// <summary>
    /// 封装Restore作用域相关的数据和行为
    /// </summary>
    private sealed class RestoreScope(Action restore) : IDisposable
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// 说明释放在当前类型中的职责
        /// </summary>
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
