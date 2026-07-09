using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Tw.AspNetCore.Mvc.Context;
using Tw.Threading;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.Context;

/// <summary>验证 HttpContextCancellationTokenProviderTests 相关行为</summary>
public class HttpContextCancellationTokenProviderTests
{
    /// <summary>验证 FakeHttpContextAccessor 相关行为</summary>
    private sealed class FakeHttpContextAccessor : IHttpContextAccessor
    {
        /// <summary>表示 HttpContext 属性</summary>
        public HttpContext? HttpContext { get; set; }
    }

    /// <summary>验证 CreateSut 场景</summary>
    /// <param name="accessor">accessor 参数</param>
    /// <returns>CreateSut 的执行结果</returns>
    private static HttpContextCancellationTokenProvider CreateSut(IHttpContextAccessor accessor)
    {
        return new HttpContextCancellationTokenProvider(
            new AsyncLocalCancellationTokenScopeProvider(),
            accessor);
    }

    /// <summary>验证 Token_ReturnsRequestAborted_WhenHttpContextExists 场景</summary>
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

    /// <summary>验证 Token_PrefersOverride_OverRequestAborted 场景</summary>
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

    /// <summary>验证 Token_ReturnsNone_WhenNoHttpContextAndNoOverride 场景</summary>
    [Fact]
    public void Token_ReturnsNone_WhenNoHttpContextAndNoOverride()
    {
        var accessor = new FakeHttpContextAccessor { HttpContext = null };
        var sut = CreateSut(accessor);

        sut.Token.Should().Be(CancellationToken.None);
    }
}
