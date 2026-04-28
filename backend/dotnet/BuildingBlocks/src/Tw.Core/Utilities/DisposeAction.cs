using Tw.Core;

namespace Tw.Core.Utilities;

/// <summary>
/// 在实例释放时调用给定操作
/// </summary>
public sealed class DisposeAction : IDisposable
{
    private Action? action;

    /// <summary>
    /// 初始化 <see cref="DisposeAction"/> 类的新实例
    /// </summary>
    /// <param name="action">释放期间要调用的操作</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="action"/> 为 <see langword="null"/> 时抛出</exception>
    public DisposeAction(Action action)
    {
        this.action = Check.NotNull(action);
    }

    /// <summary>
    /// 最多调用一次已配置的操作
    /// </summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref action, null)?.Invoke();
    }
}
