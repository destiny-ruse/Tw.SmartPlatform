using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Tw.AspNetCore.Mvc.Context;
using Tw.Threading;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.Context;

/// <summary>
/// 覆盖Http上下文Cancellation令牌提供器的核心行为和边界条件
/// </summary>
public class HttpContextCancellationTokenProviderTests
{
    /// <summary>
    /// 覆盖FakeHttp上下文Accessor的核心行为和边界条件
    /// </summary>
    private sealed class FakeHttpContextAccessor : IHttpContextAccessor
    {
        /// <summary>
        /// Http上下文在当前对象中的业务含义
        /// </summary>
        public HttpContext? HttpContext { get; set; }
    }

    /// <summary>
    /// 创建Sut测试对象
    /// </summary>
    /// <param name="accessor">用于提供accessor</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static HttpContextCancellationTokenProvider CreateSut(IHttpContextAccessor accessor)
    {
        return new HttpContextCancellationTokenProvider(
            new AsyncLocalCancellationTokenScopeProvider(),
            accessor);
    }

    /// <summary>
    /// 验证令牌返回请求Aborted当Http上下文Exists
    /// </summary>
    [Fact]
    public void Token_ReturnsRequestAborted_WhenHttpContextExists()
    {
        using var cts = new CancellationTokenSource();
        var accessor = new FakeHttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { RequestAborted = cts.Token },
        };
        var sut = CreateSut(accessor);

        sut.Token.Should().Be(cts.Token);
    }

    /// <summary>
    /// 验证令牌PrefersOverrideOver请求Aborted
    /// </summary>
    [Fact]
    public void Token_PrefersOverride_OverRequestAborted()
    {
        using var requestCts = new CancellationTokenSource();
        using var overrideCts = new CancellationTokenSource();
        var accessor = new FakeHttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { RequestAborted = requestCts.Token },
        };
        var sut = CreateSut(accessor);

        using (sut.Use(overrideCts.Token))
        {
            sut.Token.Should().Be(overrideCts.Token);
        }

        sut.Token.Should().Be(requestCts.Token);
    }

    /// <summary>
    /// 验证令牌返回None当NoHttp上下文和NoOverride
    /// </summary>
    [Fact]
    public void Token_ReturnsNone_WhenNoHttpContextAndNoOverride()
    {
        var accessor = new FakeHttpContextAccessor { HttpContext = null };
        var sut = CreateSut(accessor);

        sut.Token.Should().Be(CancellationToken.None);
    }
}
