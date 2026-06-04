namespace Tw.Localization.Caching;

/// <summary>
/// 表示本地化缓存的变更令牌，允许外部组件注册回调以响应缓存失效事件
/// </summary>
public interface ILocalizationChangeToken
{
    /// <summary>
    /// 注册一个在缓存变更时触发的回调
    /// </summary>
    /// <param name="callback">变更发生时调用的委托；参数为 <paramref name="state"/></param>
    /// <param name="state">传递给 <paramref name="callback"/> 的可选状态对象</param>
    /// <returns>用于注销回调的 <see cref="IDisposable"/>；调用 <see cref="IDisposable.Dispose"/> 后不再触发该回调</returns>
    IDisposable RegisterChangeCallback(Action<object?> callback, object? state);
}
