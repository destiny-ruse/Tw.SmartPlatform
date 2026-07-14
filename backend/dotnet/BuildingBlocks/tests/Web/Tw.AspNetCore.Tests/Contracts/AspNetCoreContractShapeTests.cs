using AwesomeAssertions;
using Tw.AspNetCore.Authentication;
using Tw.AspNetCore.Correlation;
using Tw.AspNetCore.Errors;
using Xunit;

namespace Tw.AspNetCore.Tests.Contracts;

/// <summary>
/// 固定 ASP.NET Core 提供方无关契约的公开形状
/// </summary>
public sealed class AspNetCoreContractShapeTests
{
    /// <summary>
    /// 认证方案名称保持与 ASP.NET Core 常用方案一致
    /// </summary>
    [Fact]
    public void AuthenticationSchemeNames_ExposeBearerAndCookiesSchemes()
    {
        AuthenticationSchemeNames.Bearer.Should().Be("Bearer");
        AuthenticationSchemeNames.Cookies.Should().Be("Cookies");
    }

    /// <summary>
    /// 协议错误保留状态码、稳定错误码、安全消息与追踪标识
    /// </summary>
    [Fact]
    public void ProtocolError_ExposesExpectedContractShape()
    {
        var error = new ProtocolError(422, "request.invalid", "请求内容无效", "trace-1");

        error.StatusCode.Should().Be(422);
        error.Code.Should().Be("request.invalid");
        error.Message.Should().Be("请求内容无效");
        error.TraceId.Should().Be("trace-1");
        error.Should().Be(new ProtocolError(422, "request.invalid", "请求内容无效", "trace-1"));
    }

    /// <summary>
    /// 冲突工厂固定返回 HTTP 409 并原样保留调用方提供的安全字段
    /// </summary>
    [Fact]
    public void ProtocolErrorConflict_ReturnsConflictContract()
    {
        var error = ProtocolError.Conflict("request.conflict", "请求状态冲突", "trace-2");

        error.Should().Be(new ProtocolError(409, "request.conflict", "请求状态冲突", "trace-2"));
    }

    /// <summary>
    /// 请求关联契约同时保留追踪标识与业务关联标识
    /// </summary>
    [Fact]
    public void RequestCorrelation_ExposesTraceAndCorrelationIdentifiers()
    {
        var correlation = new RequestCorrelation("trace-3", "correlation-3");

        correlation.TraceId.Should().Be("trace-3");
        correlation.CorrelationId.Should().Be("correlation-3");
        correlation.Should().Be(new RequestCorrelation("trace-3", "correlation-3"));
    }
}
