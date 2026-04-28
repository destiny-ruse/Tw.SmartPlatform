namespace Tw.Core.Utilities;

/// <summary>
/// 提供释放时无任何效果的可复用释放实例
/// </summary>
public sealed class NullDisposable : IDisposable
{
    /// <summary>
    /// 共享的空操作释放实例
    /// </summary>
    public static NullDisposable Instance { get; } = new();

    private NullDisposable()
    {
    }

    /// <summary>
    /// 不执行任何操作
    /// </summary>
    public void Dispose()
    {
    }
}
