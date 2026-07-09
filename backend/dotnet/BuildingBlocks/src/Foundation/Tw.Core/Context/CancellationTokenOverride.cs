namespace Tw.Context;

/// <summary>
/// 表示当前异步调用链作用域内覆盖的取消令牌
/// </summary>
public sealed class CancellationTokenOverride
{
    /// <summary>
    /// 初始化 <see cref="CancellationTokenOverride"/> 类的新实例
    /// </summary>
    /// <param name="cancellationToken">作用域内生效的取消令牌</param>
    public CancellationTokenOverride(CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// 当前作用域生效的取消令牌
    /// </summary>
    /// <value>入口层通过作用域写入的取消令牌</value>
    public CancellationToken CancellationToken { get; }
}
