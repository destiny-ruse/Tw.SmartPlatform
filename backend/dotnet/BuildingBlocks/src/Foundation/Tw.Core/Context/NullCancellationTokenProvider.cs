namespace Tw.Context;

/// <summary>
/// 为非 Web 入口和未显式设置令牌的场景提供稳定默认值的取消令牌 provider
/// </summary>
/// <remarks>没有覆盖令牌时返回 <see cref="CancellationToken.None"/>；存在覆盖令牌时返回覆盖令牌。</remarks>
public sealed class NullCancellationTokenProvider : CancellationTokenProviderBase
{
    /// <summary>
    /// 初始化 <see cref="NullCancellationTokenProvider"/> 类的新实例
    /// </summary>
    /// <param name="scopeProvider">维护异步作用域的取消令牌作用域 provider</param>
    public NullCancellationTokenProvider(AsyncLocalCancellationTokenScopeProvider scopeProvider)
        : base(scopeProvider)
    {
    }

    /// <inheritdoc />
    public override CancellationToken Token => OverrideValue ?? CancellationToken.None;
}
