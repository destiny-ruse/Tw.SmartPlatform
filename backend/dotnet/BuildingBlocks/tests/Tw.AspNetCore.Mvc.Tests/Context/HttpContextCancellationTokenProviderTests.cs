using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Tw.AspNetCore.Mvc.Context;
using Tw.Context;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.Context;

public class HttpContextCancellationTokenProviderTests
{
    private sealed class FakeHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    private static HttpContextCancellationTokenProvider CreateSut(IHttpContextAccessor accessor)
    {
        return new HttpContextCancellationTokenProvider(
            new AsyncLocalCancellationTokenScopeProvider(),
            accessor);
    }

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

    [Fact]
    public void Token_ReturnsNone_WhenNoHttpContextAndNoOverride()
    {
        var accessor = new FakeHttpContextAccessor { HttpContext = null };
        var sut = CreateSut(accessor);

        sut.Token.Should().Be(CancellationToken.None);
    }
}
