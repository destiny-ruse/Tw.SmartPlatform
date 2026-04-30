namespace Tw.DependencyInjection.Cancellation;

/// <summary>
/// 提供当前调用链的取消令牌；可被任意服务注入。
/// </summary>
public interface ICancellationTokenProvider
{
    /// <summary>
    /// 当前活跃的取消令牌；无活跃 token 时返回 <see cref="CancellationToken.None"/>。
    /// </summary>
    CancellationToken Token { get; }
}
