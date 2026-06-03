namespace Tw.Context;

/// <summary>
/// 取消令牌 provider 的抽象基类，封装作用域覆盖读取与 <see cref="ICancellationTokenProvider.Use"/> 实现
/// </summary>
/// <remarks>派生类只需提供自身默认令牌来源；覆盖令牌优先级高于派生类默认令牌。</remarks>
public abstract class CancellationTokenProviderBase : ICancellationTokenProvider
{
    /// <summary>
    /// 初始化 <see cref="CancellationTokenProviderBase"/> 类的新实例
    /// </summary>
    /// <param name="scopeProvider">维护异步作用域的取消令牌作用域 provider</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="scopeProvider"/> 为 <see langword="null"/> 时抛出</exception>
    protected CancellationTokenProviderBase(AsyncLocalCancellationTokenScopeProvider scopeProvider)
    {
        ScopeProvider = Check.NotNull(scopeProvider);
    }

    /// <summary>
    /// 维护异步作用域的取消令牌作用域 provider
    /// </summary>
    protected AsyncLocalCancellationTokenScopeProvider ScopeProvider { get; }

    /// <summary>
    /// 当前作用域的覆盖令牌
    /// </summary>
    /// <value>没有活动作用域时为 <see langword="null"/></value>
    protected CancellationToken? OverrideValue => ScopeProvider.Current?.CancellationToken;

    /// <inheritdoc />
    public abstract CancellationToken Token { get; }

    /// <inheritdoc />
    public IDisposable Use(CancellationToken cancellationToken)
    {
        return ScopeProvider.BeginScope(cancellationToken);
    }
}
