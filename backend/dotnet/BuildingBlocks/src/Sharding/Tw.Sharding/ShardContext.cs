using Tw.Sharding.Abstractions;

namespace Tw.Sharding;

public sealed class ShardContext : IShardContext
{
    private readonly AsyncLocal<ShardDescriptor?> _current = new();

    public ShardDescriptor Current => _current.Value ?? ShardDescriptor.None;

    public IDisposable Change(ShardDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var previous = _current.Value;
        _current.Value = descriptor;
        return new RestoreScope(() => _current.Value = previous);
    }

    private sealed class RestoreScope(Action restore) : IDisposable
    {
        private bool _disposed;

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
