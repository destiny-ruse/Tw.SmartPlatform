namespace Tw.DependencyInjection.Cancellation;

/// <summary>
/// 切换当前调用链取消令牌的访问器；返回的 <see cref="IDisposable"/> 恢复原值。
/// </summary>
public interface ICurrentCancellationTokenAccessor
{
    /// <summary>
    /// 设置 ambient token 至 <paramref name="token"/>，Dispose 时恢复原值。
    /// 调用方应以 LIFO 顺序 Dispose，否则行为未定义。
    /// </summary>
    IDisposable Use(CancellationToken token);
}
