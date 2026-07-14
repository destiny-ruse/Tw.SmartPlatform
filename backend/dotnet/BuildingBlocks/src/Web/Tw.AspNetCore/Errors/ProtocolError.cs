namespace Tw.AspNetCore.Errors;

/// <summary>
/// 描述可由 ASP.NET Core 入口适配器映射的协议错误
/// </summary>
public sealed record ProtocolError
{
    /// <summary>
    /// 保存经过校验的稳定错误码
    /// </summary>
    private string _code = string.Empty;

    /// <summary>
    /// 保存经过校验的安全错误消息
    /// </summary>
    private string _message = string.Empty;

    /// <summary>
    /// 初始化入口适配器可映射的协议错误
    /// </summary>
    /// <param name="statusCode">对外返回的 HTTP 状态码</param>
    /// <param name="code">供调用方稳定识别的非空白错误码</param>
    /// <param name="message">不包含内部实现细节的非空白安全错误消息</param>
    /// <param name="traceId">关联日志与链路追踪的可选标识</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="code"/> 或 <paramref name="message"/> 为 <see langword="null"/> 时抛出
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="code"/> 或 <paramref name="message"/> 为空白字符串时抛出
    /// </exception>
    public ProtocolError(int statusCode, string code, string message, string? traceId)
    {
        StatusCode = statusCode;
        _code = ValidateRequiredText(code, nameof(code), "协议错误码");
        _message = ValidateRequiredText(message, nameof(message), "协议错误消息");
        TraceId = traceId;
    }

    /// <summary>
    /// 对外返回的 HTTP 状态码
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// 供调用方稳定识别的非空白错误码
    /// </summary>
    /// <exception cref="ArgumentNullException">init 值为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">init 值为空白字符串时抛出</exception>
    public string Code
    {
        get => _code;
        init => _code = ValidateRequiredText(value, nameof(Code), "协议错误码");
    }

    /// <summary>
    /// 不包含内部实现细节的非空白安全错误消息
    /// </summary>
    /// <exception cref="ArgumentNullException">init 值为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">init 值为空白字符串时抛出</exception>
    public string Message
    {
        get => _message;
        init => _message = ValidateRequiredText(value, nameof(Message), "协议错误消息");
    }

    /// <summary>
    /// 关联日志与链路追踪的可选标识
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// 创建幂等请求冲突结果
    /// </summary>
    /// <param name="code">对外返回的非空白稳定错误码</param>
    /// <param name="message">对外返回的非空白安全错误消息</param>
    /// <param name="traceId">用于关联请求链路的 trace 标识</param>
    /// <returns>HTTP 状态码固定为 409 的协议错误</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="code"/> 或 <paramref name="message"/> 为 <see langword="null"/> 时抛出
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="code"/> 或 <paramref name="message"/> 为空白字符串时抛出
    /// </exception>
    public static ProtocolError Conflict(string code, string message, string? traceId = null)
    {
        return new ProtocolError(409, code, message, traceId);
    }

    /// <summary>
    /// 校验协议错误必填文本并保留原始值
    /// </summary>
    /// <param name="value">需要校验的协议错误文本</param>
    /// <param name="parameterName">写入异常的构造参数或属性名称</param>
    /// <param name="displayName">用于中文异常消息的字段名称</param>
    /// <returns>通过非空白校验的原始文本</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> 为空白字符串时抛出</exception>
    private static string ValidateRequiredText(
        string? value,
        string parameterName,
        string displayName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName, $"{displayName}不能为空");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{displayName}不能为空白", parameterName);
        }

        return value;
    }
}
