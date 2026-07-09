namespace Tw.Threading;

/// <summary>
/// 提供当前执行上下文的取消令牌，并允许入口层临时覆盖该令牌
/// </summary>
public interface ICancellationTokenProvider
{
    /// <summary>
    /// 当前执行上下文的取消令牌
    /// </summary>
    /// <value>没有活动作用域时由具体实现决定默认值</value>
    CancellationToken Token { get; }

    /// <summary>
    /// 在当前异步调用链内建立取消令牌作用域
    /// </summary>
    /// <param name="cancellationToken">作用域内生效的取消令牌</param>
    /// <returns>释放后恢复外层作用域的 <see cref="IDisposable"/></returns>
    /// <remarks>支持嵌套作用域；释放内层作用域后恢复外层令牌。</remarks>
    IDisposable Use(CancellationToken cancellationToken);
}
