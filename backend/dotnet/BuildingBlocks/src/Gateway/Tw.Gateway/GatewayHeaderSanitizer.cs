namespace Tw.Gateway;

/// <summary>
/// 封装GatewayHeaderSanitizer相关的数据和行为
/// </summary>
public static class GatewayHeaderSanitizer
{
    /// <summary>
    /// 说明Sanitize在当前类型中的职责
    /// </summary>
    /// <param name="headers">用于提供headers</param>
    /// <returns>方法计算得到的文本值</returns>
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
