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
    /// 协议错误构造函数拒绝 null 或空白错误码
    /// </summary>
    /// <param name="code">需要验证的错误码</param>
    /// <param name="exceptionType">无效输入对应的精确异常类型</param>
    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(ArgumentException))]
    [InlineData("  ", typeof(ArgumentException))]
    public void ProtocolErrorConstructor_RejectsInvalidCode(
        string? code,
        Type exceptionType)
    {
        Action act = () => _ = new ProtocolError(400, code!, "请求无效", null);

        AssertInvalidRequiredText(act, exceptionType, "code", "协议错误码不能为空");
    }

    /// <summary>
    /// 协议错误构造函数拒绝 null 或空白安全消息
    /// </summary>
    /// <param name="message">需要验证的安全消息</param>
    /// <param name="exceptionType">无效输入对应的精确异常类型</param>
    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(ArgumentException))]
    [InlineData("  ", typeof(ArgumentException))]
    public void ProtocolErrorConstructor_RejectsInvalidMessage(
        string? message,
        Type exceptionType)
    {
        Action act = () => _ = new ProtocolError(400, "request.invalid", message!, null);

        AssertInvalidRequiredText(act, exceptionType, "message", "协议错误消息不能为空");
    }

    /// <summary>
    /// 冲突工厂拒绝 null 或空白错误码
    /// </summary>
    /// <param name="code">需要验证的错误码</param>
    /// <param name="exceptionType">无效输入对应的精确异常类型</param>
    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(ArgumentException))]
    [InlineData("  ", typeof(ArgumentException))]
    public void ProtocolErrorConflict_RejectsInvalidCode(
        string? code,
        Type exceptionType)
    {
        Action act = () => _ = ProtocolError.Conflict(code!, "请求冲突");

        AssertInvalidRequiredText(act, exceptionType, "code", "协议错误码不能为空");
    }

    /// <summary>
    /// 冲突工厂拒绝 null 或空白安全消息
    /// </summary>
    /// <param name="message">需要验证的安全消息</param>
    /// <param name="exceptionType">无效输入对应的精确异常类型</param>
    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(ArgumentException))]
    [InlineData("  ", typeof(ArgumentException))]
    public void ProtocolErrorConflict_RejectsInvalidMessage(
        string? message,
        Type exceptionType)
    {
        Action act = () => _ = ProtocolError.Conflict("request.conflict", message!);

        AssertInvalidRequiredText(act, exceptionType, "message", "协议错误消息不能为空");
    }

    /// <summary>
    /// init 更新不能把错误码改为空白值
    /// </summary>
    [Fact]
    public void ProtocolErrorWithExpression_RejectsInvalidCode()
    {
        var error = new ProtocolError(400, "request.invalid", "请求无效", null);
        Action act = () => _ = error with { Code = " " };

        AssertInvalidRequiredText(
            act,
            typeof(ArgumentException),
            nameof(ProtocolError.Code),
            "协议错误码不能为空");
    }

    /// <summary>
    /// init 更新不能把安全消息改为 null
    /// </summary>
    [Fact]
    public void ProtocolErrorWithExpression_RejectsNullMessage()
    {
        var error = new ProtocolError(400, "request.invalid", "请求无效", null);
        Action act = () => _ = error with { Message = null! };

        AssertInvalidRequiredText(
            act,
            typeof(ArgumentNullException),
            nameof(ProtocolError.Message),
            "协议错误消息不能为空");
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

    /// <summary>
    /// 断言必填协议文本使用精确异常类型、参数名和中文消息拒绝无效值
    /// </summary>
    /// <param name="act">触发协议文本校验的操作</param>
    /// <param name="exceptionType">预期的精确异常类型</param>
    /// <param name="parameterName">预期写入异常的参数名</param>
    /// <param name="messagePrefix">预期中文异常消息前缀</param>
    private static void AssertInvalidRequiredText(
        Action act,
        Type exceptionType,
        string parameterName,
        string messagePrefix)
    {
        var exception = act.Should().Throw<ArgumentException>().Which;

        exception.GetType().Should().Be(exceptionType);
        exception.ParamName.Should().Be(parameterName);
        exception.Message.Should().StartWith(messagePrefix);
    }
}
