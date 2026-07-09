namespace Tw.Threading;

/// <summary>
/// 为 <see cref="ICancellationTokenProvider"/> 提供显式令牌优先的统一取值方法
/// </summary>
public static class CancellationTokenProviderExtensions
{
    /// <summary>
    /// 在显式令牌缺省时回退到 provider 当前令牌
    /// </summary>
    /// <param name="provider">取消令牌 provider</param>
    /// <param name="preferredValue">调用方显式传入的取消令牌</param>
    /// <returns>显式令牌存在时返回显式令牌，否则返回 <see cref="ICancellationTokenProvider.Token"/></returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="provider"/> 为 <see langword="null"/> 时抛出</exception>
    /// <remarks>
    /// 当 <paramref name="preferredValue"/> 等于 <see langword="default"/> 或 <see cref="CancellationToken.None"/> 时视为缺省。
    /// </remarks>
    public static CancellationToken FallbackToProvider(
        this ICancellationTokenProvider provider,
        CancellationToken preferredValue = default)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return preferredValue == default || preferredValue == CancellationToken.None
            ? provider.Token
            : preferredValue;
    }
}
