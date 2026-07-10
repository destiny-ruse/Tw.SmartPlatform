using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tw.AspNetCore.Mvc.Context;
using Tw.Threading;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.Context;

/// <summary>
/// 覆盖Cancellation令牌服务CollectionExtensions的核心行为和边界条件
/// </summary>
public class CancellationTokenServiceCollectionExtensionsTests
{
    /// <summary>
    /// 验证添加Http上下文Cancellation令牌提供器Replaces提供器带有Http上下文提供器
    /// </summary>
    [Fact]
    public void AddHttpContextCancellationTokenProvider_ReplacesProvider_WithHttpContextProvider()
    {
        var services = new ServiceCollection();

        services.AddHttpContextCancellationTokenProvider();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICancellationTokenProvider>()
            .Should().BeOfType<HttpContextCancellationTokenProvider>();
    }

    /// <summary>
    /// 验证添加Http上下文Cancellation令牌提供器注册Http上下文Accessor
    /// </summary>
    [Fact]
    public void AddHttpContextCancellationTokenProvider_RegistersHttpContextAccessor()
    {
        var services = new ServiceCollection();

        services.AddHttpContextCancellationTokenProvider();

        using var provider = services.BuildServiceProvider();
        provider.GetService<IHttpContextAccessor>().Should().NotBeNull();
    }

    /// <summary>
    /// 验证添加Http上下文Cancellation令牌提供器注册作用域提供器作为Singleton
    /// </summary>
    [Fact]
    public void AddHttpContextCancellationTokenProvider_RegistersScopeProvider_AsSingleton()
    {
        var services = new ServiceCollection();

        services.AddHttpContextCancellationTokenProvider();

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<AsyncLocalCancellationTokenScopeProvider>();
        var second = provider.GetRequiredService<AsyncLocalCancellationTokenScopeProvider>();
        first.Should().BeSameAs(second);
    }
}
