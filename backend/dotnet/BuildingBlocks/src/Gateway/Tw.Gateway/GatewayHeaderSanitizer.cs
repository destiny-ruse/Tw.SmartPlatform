namespace Tw.Gateway;

/// <summary>表示 GatewayHeaderSanitizer 类型</summary>
public static class GatewayHeaderSanitizer
{
    /// <summary>执行 Sanitize 操作</summary>
    /// <param name="headers">headers 参数</param>
    /// <returns>Sanitize 的执行结果</returns>
    public static IReadOnlyDictionary<string, string> Sanitize(IReadOnlyDictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        var sanitized = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
        foreach (var header in GatewayHeaderPolicy.CallerSuppliedIdentityHeaders)
        {
            sanitized.Remove(header);
        }

        return sanitized;
    }
}
