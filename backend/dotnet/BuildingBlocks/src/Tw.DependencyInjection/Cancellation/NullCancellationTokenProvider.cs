namespace Tw.DependencyInjection.Cancellation;

/// <summary>
/// 默认空提供者，永远返回 <see cref="CancellationToken.None"/>。
/// 用作 DI 容器中的回退实现，确保任何服务都能注入 <see cref="ICancellationTokenProvider"/>。
/// </summary>
public sealed class NullCancellationTokenProvider : ICancellationTokenProvider
{
    /// <inheritdoc />
    public CancellationToken Token => CancellationToken.None;
}
